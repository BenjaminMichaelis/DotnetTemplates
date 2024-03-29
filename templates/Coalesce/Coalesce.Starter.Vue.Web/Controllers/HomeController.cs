using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace Coalesce.Starter.Vue.Web.Controllers;

public class HomeController : Controller
{
    /// <summary>
    /// Spa route for vue-based parts of the app
    /// </summary>
    // Prevent caching of this route.
    // The served file will contain the links to compiled js/css that include hashes in the filenames.
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> IndexAsync(
        [FromServices] IWebHostEnvironment hostingEnvironment
    )
    {
        IFileInfo fileInfo = hostingEnvironment.WebRootFileProvider.GetFileInfo("index.html");
        if (!fileInfo.Exists)
        {
            ContentResult result = Content($"<h1>{{hostingEnvironment.WebRootPath}}/index.html was not found. First time build is probably still running. Refresh in a few seconds...</h1>", "text/html");
            result.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return result;
        }
        if (!fileInfo.Exists) return NotFound();

        // If desired, you can inject settings or other variables into index.html here.
        // These will then be available as global variables in your Vue app:
        using var reader = new StreamReader(fileInfo.CreateReadStream());
        string contents = await reader.ReadToEndAsync();
        contents = contents.Replace("ENVIRONMENT_NAME = null;", $"ENVIRONMENT_NAME = \"{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}\";");

        return Content(contents, "text/html");

    }

    public IActionResult Error()
    {
        ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View();
    }
}
