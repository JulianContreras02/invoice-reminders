using InvoiceReminders.Application.Email;
using InvoiceReminders.Application.Reminders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiceReminders.Application;

public static class DependencyInjection
{
    /// <summary>Registra los casos de uso y servicios puros de la capa de aplicación.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReminderEmailTemplate, SecondReminderTemplate>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReminderEmailTemplate, DeactivationNoticeTemplate>());
        services.AddSingleton<IReminderEmailComposer, ReminderEmailComposer>();

        services.AddScoped<ProcessRemindersUseCase>();
        return services;
    }
}
