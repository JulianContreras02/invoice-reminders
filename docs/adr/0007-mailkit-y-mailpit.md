# ADR 0007: Transporte con MailKit y capturador Mailpit

**Estado:** Aceptada · **Fecha:** 2026-10-21

## Contexto

El enunciado exige **enviar un correo** al cliente antes de cambiar el estado; un simple log no lo cumpliría. El equipo revisor debe poder ver los correos sin credenciales ni acceso a un proveedor externo, en la misma máquina donde ejecuta `docker compose up --build`. La semilla usa el dominio reservado `.test` para que nunca se envíe a direcciones reales.

El candidato tenía como preferencia inicial MailHog, cuyo último release es de 2020-08-11 (último push en 2024-02). Mailpit cumple el mismo rol con mantenimiento activo (release v1.31.6 del 2026-10-15, cadencia semanal), los mismos puertos (SMTP 1025, UI 8025) y una API REST.

## Decisión

- **Transporte: MailKit 4.18.1.** `MailKitEmailSender` (Infrastructure) crea un `MailKit.Net.Smtp.SmtpClient` **por envío** (no es thread-safe), hace `ConnectAsync(host, port, ToSecureSocketOptions(Security))`, autentica solo si hay usuario, construye el `MimeMessage` con `new MailboxAddress(message.ToName, message.ToAddress)`, `MailboxAddress(FromName, FromAddress)` y el `Message-ID` de `EmailMessage`, y hace `SendAsync`. Se registra como Singleton de `IEmailSender`.
- **El correo cuenta como entregado en cuanto el servidor acepta el mensaje.** El cierre de la conexión (`DisconnectAsync(true, CancellationToken.None)`) va aparte y un fallo ahí solo se registra en Debug: tratarlo como "no enviado" haría que la siguiente ejecución duplicara el correo. El log de envío registra el `Message-ID`, no la dirección del destinatario.
- La regla `IsDeliveryFailure` (`internal static`, probada en `MailKitEmailSenderTests`) clasifica como fallo de entrega `SocketException`, `IOException`, `TimeoutException`, `SmtpCommandException`, `SmtpProtocolException`, `AuthenticationException`, `SslHandshakeException`, `ServiceNotConnectedException`, `ServiceNotAuthenticatedException`, `ParseException` y el `OperationCanceledException` que no pidió quien llama (timeout); esas excepciones se traducen a `EmailDeliveryException` con un mensaje genérico, sin datos personales ni de infraestructura ("El servidor de correo no está disponible o rechazó el mensaje."), y la causa técnica como excepción interna, que solo llega al log. Una cancelación pedida por quien llama se deja propagar y los errores de programación no se ocultan. El contrato de `IEmailSender` es `Task SendAsync(EmailMessage, CancellationToken)`: termina sin excepción si el servidor aceptó el mensaje o lanza `EmailDeliveryException`; nunca éxito parcial.
- `SmtpOptions { Host, Port = 1025, FromAddress, FromName, Security = SmtpSecurity.None, Username?, Password?, TimeoutSeconds = 10 }` con `enum SmtpSecurity { None, StartTls, SslOnConnect, Auto }` mapeado a `SecureSocketOptions`; validación cruzada de usuario y contraseña. Un SMTP real se configura solo con variables (`Smtp__Host`, `Smtp__Port=587` + `Smtp__Security=StartTls`, o `Smtp__Port=465` + `Smtp__Security=SslOnConnect`, `Smtp__Username`, `Smtp__Password`); nunca se versionan credenciales.
- **Capturador: Mailpit** (`axllent/mailpit:v1.31.6`) en compose, con `Smtp__Host: mailpit`, `Smtp__Port: 1025`, `Smtp__Security: None`, sin variables `MP_SMTP_AUTH_*`. UI en `http://localhost:8025`; su API REST permite vaciar la bandeja (`DELETE http://localhost:8025/api/v1/messages`, o botón *Delete all*).
- **Sin `LoggingEmailSender`** ni ningún emisor simulado registrable en producción.
- **Composición en Application**, pura y probada por unidad: `ReminderEmailComposer` selecciona la plantilla por `ReminderKind` (y falla al construirse si falta alguna), formatea el monto en `es-CO` (`N0`, moneda como texto) y la fecha `dd/MM/yyyy`, fija el `Message-ID` determinista `{numeroFactura}.{estadoAnterior}.{runId sin guiones}@invoice-reminders.test` (sin corchetes) y las cabeceras `X-Invoice-Id`, `X-Reminder-Kind`, `X-Reminder-Run-Id`. La validez del correo del destinatario la decide el dominio con `Customer.HasDeliverableEmail()` (`MailAddress.TryCreate` y, además, la dirección parseada debe coincidir con la cadena, lo que rechaza `"Nombre <correo>"`); `ProcessRemindersUseCase` la consulta antes de componer. Dos plantillas (`SecondReminderTemplate`, `DeactivationNoticeTemplate`) registradas como colección de `IReminderEmailTemplate`; cada una **interpola** `ReminderEmailModel(CustomerName, InvoiceNumber, AmountFormatted, Currency, DueOnFormatted)` en un asunto y unos párrafos, y `ReminderEmailLayout` genera con ellos el texto plano y el HTML sencillo, codificando con `WebUtility.HtmlEncode`; el resultado es `RenderedEmail(Subject, TextBody, HtmlBody)` y el correo sale multipart texto + HTML. Remitente `Recordatorios de facturación <recordatorios@invoice-reminders.test>`.
- `SmtpTcpHealthCheck` (`TcpClient.ConnectAsync`, timeout 2 s) reporta `Degraded` en `/health/ready` cuando Mailpit no responde.

## Alternativas consideradas

- **MailHog** (preferencia inicial del candidato). Misma función y puertos, pero sin releases desde 2020. Como el código no cambia (solo host y puerto), revertir a MailHog no tiene coste si hiciera falta.
- **`System.Net.Mail.SmtpClient`.** La documentación de Microsoft lo desaconseja; MailKit es la recomendación oficial. Descartado.
- **Emisor simulado (`LoggingEmailSender`).** Un emisor que devuelve éxito sin enviar permitiría avanzar estados sin correo si alguien lo dejara configurado. Mailpit cubre la depuración sin red externa. Descartado.
- **Motor de plantillas (Razor, Scriban).** Dos textos fijos no justifican una dependencia más; la interpolación con `HtmlEncode` se prueba por unidad con el nombre `Pérez & Asociados S.A.S.`.
- **Proveedor SMTP externo en la demo.** Exigiría credenciales al revisor y enviaría correos fuera de su máquina. Descartado; sigue siendo posible solo con variables.

## Consecuencias

Positivas:

- El revisor ve 7 correos reales (3 destinatarios, 2 asuntos) en una UI web sin configurar nada.
- `docker compose stop mailpit` reproduce CA-08 de forma natural: `EmailDeliveryException` → `EmailFailed`, estado intacto, chip "SMTP no disponible".
- Nombre y dirección del destinatario viajan separados (`EmailMessage.ToName`, `ToAddress`), así que `Pérez & Asociados S.A.S.` no rompe el parseo.
- Cambiar a un SMTP real es cuestión de variables de entorno, incluido TLS implícito en 465.

Negativas y lo que se acepta a cambio:

- De `MailKitEmailSender` solo se prueba por unidad la regla `IsDeliveryFailure`; el envío real no (sería probar MailKit): se valida contra Mailpit y así se declara.
- Mailpit conserva los correos al reiniciar los datos; vaciarlo es un paso documentado (README, `docs/VERIFICACION.md`, nota en `RunResultPanel`) y no se acopla al endpoint de reinicio.
- La imagen de Mailpit declara `HEALTHCHECK` (`/mailpit readyz`), verificado en su Dockerfile de la versión v1.31.6; `app` espera con `service_healthy`.
- Si se revirtiera a MailHog habría que revisar el comando documentado para vaciar la bandeja; el código no depende de la API REST de Mailpit.

## Referencias

- `docs/ARQUITECTURA.md`: plantillas de correo y tabla DI (`IEmailSender` Singleton).
- `README.md`: variables `Smtp__*`, URL de Mailpit y paso "vaciar Mailpit".
- ADR 0004 (orden envío → actualización) y ADR 0009 (compose).
