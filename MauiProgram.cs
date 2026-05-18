using DBService;
using Microsoft.Extensions.Logging;

namespace MauiApp6
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<LocalDBService>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<DatabaseUpdatePage>();

#if DEBUG
                        builder.Logging.AddDebug();


#endif
            var app= builder.Build();
#if DEBUG

            var dbService = app.Services.GetRequiredService<LocalDBService>();
            Task.Run(() => dbService.SeedAsync()).GetAwaiter().GetResult();

#endif

            return app;
        }
    }
}
