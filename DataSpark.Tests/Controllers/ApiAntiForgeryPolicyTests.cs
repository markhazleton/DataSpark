using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace DataSpark.Tests.Controllers;

[TestClass]
public class ApiAntiForgeryPolicyTests
{
    [TestMethod]
    public void ChartApiController_WithHttpPostActions_ShouldApplyValidateAntiForgeryToken()
    {
        AssertHttpPostActionsRequireAntiForgery(typeof(DataSpark.Web.Controllers.Api.ChartApiController));
    }

    [TestMethod]
    public void FilesController_WithHttpPostActions_ShouldApplyValidateAntiForgeryToken()
    {
        AssertHttpPostActionsRequireAntiForgery(typeof(DataSpark.Web.Controllers.api.FilesController));
    }

    private static void AssertHttpPostActionsRequireAntiForgery(Type controllerType)
    {
        var postActions = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttribute<HttpPostAttribute>() != null)
            .ToList();

        postActions.Should().NotBeEmpty();

        var missing = postActions
            .Where(method => method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() == null)
            .Select(method => method.Name)
            .ToList();

        missing.Should().BeEmpty($"all [HttpPost] actions in {controllerType.Name} must include [ValidateAntiForgeryToken]");
    }
}
