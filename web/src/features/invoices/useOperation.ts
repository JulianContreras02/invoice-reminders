import { useCallback, useState } from 'react';
import { isConflict, messageOf } from '../../api/errors';

/**
 * Ejecuta una operación de escritura (procesar recordatorios, reiniciar datos) de una en una.
 * Un 409 de la API (otra operación en curso) se muestra como aviso, no como error.
 */
export function useOperation(onCompleted: () => void) {
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const run = useCallback(
    async (operation: () => Promise<string>) => {
      setBusy(true);
      setNotice(null);
      setError(null);
      try {
        setNotice(await operation());
        onCompleted();
      } catch (caught) {
        if (isConflict(caught)) {
          setNotice(caught.message);
        } else {
          setError(messageOf(caught));
        }
      } finally {
        setBusy(false);
      }
    },
    [onCompleted],
  );

  return { busy, notice, error, run };
}
