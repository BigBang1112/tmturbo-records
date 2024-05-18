using System.Net.Mime;
using System.Text;

namespace TMTurboRecords.Extensions;

internal static class ScalarExtensions
{
    public static IEndpointConventionBuilder MapScalarUi(this IEndpointRouteBuilder builder)
    {
        return builder.MapGet("/scalar/{documentName}", (HttpContext context, string documentName) =>
        {
            var html = @$"<!doctype html>
<html>
  <head>
    <title>Scalar API Reference</title>
    <meta charset=""utf-8"" />
    <meta
      name=""viewport""
      content=""width=device-width, initial-scale=1"" />
  </head>
  <body>
    <!-- Add your own OpenAPI/Swagger specification URL here: -->
    <!-- Note: The example is our public proxy (to avoid CORS issues). You can remove the `data-proxy-url` attribute if you don’t need it. -->
    <script
      id=""api-reference""
      data-url=""/swagger/{documentName}/swagger.json""></script>

    <!-- Optional: You can set a full configuration object like this: -->
    <script>
      var configuration = {{
        theme: 'purple',
      }}

      document.getElementById('api-reference').dataset.configuration =
        JSON.stringify(configuration)
    </script>
    <script src=""https://cdn.jsdelivr.net/npm/@scalar/api-reference""></script>
  </body>
</html>";

            context.Response.ContentType = MediaTypeNames.Text.Html;
            context.Response.ContentLength = Encoding.UTF8.GetByteCount(html);
            return context.Response.WriteAsync(html);

        }).ExcludeFromDescription();
    }
}
