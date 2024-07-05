
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace JuegoFramework.Helpers
{
    public class RouteExecutor
    {
        public static Task<IActionResult> Execute(string method, string route, string bodyJSON, string? accessToken = null)
        {
            var routeFound = Global.EndpointSources!
            .SelectMany(es => es.Endpoints)
            .OfType<RouteEndpoint>().Where(
                e =>
                {
                    return e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods?[0].ToLower() == method.ToLower() &&
                    e.RoutePattern.RawText == route;
                }
            ).FirstOrDefault() ?? throw new Exception("Route not found");

            var controllerMetadata = routeFound.Metadata
                .OfType<ControllerActionDescriptor>()
                .FirstOrDefault() ?? throw new Exception("Route not found");

            var controllerType = controllerMetadata.ControllerTypeInfo.AsType();
            MethodInfo methodInfo = controllerMetadata.MethodInfo;

            var controller = ActivatorUtilities.CreateInstance(Global.ServiceProvider!, controllerType) as ControllerBase ?? throw new Exception("Route not found");

            if (accessToken != null)
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                };

                controller.ControllerContext.HttpContext.Request.Headers.Append("access_token", accessToken);
            }

            var parameterInfo = methodInfo.GetParameters().FirstOrDefault();

            object[] parameters = [];
            if (parameterInfo != null)
            {
                var parameterType = parameterInfo.ParameterType;

                var deserializedParameter = JsonSerializer.Deserialize(bodyJSON, parameterType) ?? throw new Exception("Unable to parse bodyJSON to destination route method parameter");

                parameters = [deserializedParameter];
            }

            var result = methodInfo.Invoke(controller, parameters) as Task<IActionResult>;

            return result ?? throw new Exception("Route method invocation returned null");
        }
    }
}
