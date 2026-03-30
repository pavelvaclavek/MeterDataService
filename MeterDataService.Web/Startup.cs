using System.IO;
using System.Net.Http.Headers;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Owin;

namespace MeterDataService.Web
{
    /// <summary>
    /// OWIN Startup třída - konfigurace Web API a statických souborů.
    /// </summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Konfigurace Web API
            var config = new HttpConfiguration();

            // Povolení atributového routování (Route atributy na controllerech)
            config.MapHttpAttributeRoutes();

            // Výchozí konvenční route
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // JSON formátování - camelCase a přehledný výstup
            config.Formatters.JsonFormatter.SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include
            };

            // Výchozí odpověď v JSON (místo XML)
            config.Formatters.JsonFormatter.SupportedMediaTypes
                .Add(new MediaTypeHeaderValue("text/html"));

            app.UseWebApi(config);

            // Konfigurace servování statických souborů z wwwroot
            var wwwrootPath = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "wwwroot");

            if (Directory.Exists(wwwrootPath))
            {
                var fileSystem = new PhysicalFileSystem(wwwrootPath);
                var options = new FileServerOptions
                {
                    FileSystem = fileSystem,
                    RequestPath = PathString.Empty,
                    EnableDefaultFiles = true
                };
                options.DefaultFilesOptions.DefaultFileNames.Clear();
                options.DefaultFilesOptions.DefaultFileNames.Add("index.html");
                options.StaticFileOptions.ServeUnknownFileTypes = false;

                app.UseFileServer(options);
            }
        }
    }
}
