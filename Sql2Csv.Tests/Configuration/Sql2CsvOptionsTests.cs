using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sql2Csv.Core.Configuration;
using FluentAssertions;

namespace Sql2Csv.Tests.Configuration;

[TestClass]
public class Sql2CsvOptionsTests
{
    [TestMethod]
    public void Sql2CsvOptions_DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var options = new Sql2CsvOptions();

        // Assert
        options.RootPath.Should().BeEmpty();
        options.Paths.Should().NotBeNull();
        options.Database.Should().NotBeNull();
        options.Export.Should().NotBeNull();
        Sql2CsvOptions.SectionName.Should().Be("Sql2Csv");
    }

    [TestMethod]
    public void Sql2CsvOptions_Properties_ShouldBeSettable()
    {
        // Arrange
        var options = new Sql2CsvOptions();
        var rootPath = @"C:\TestPath";
        var paths = new PathOptions();
        var database = new DatabaseOptions();
        var export = new ExportOptions();

        // Act
        options.RootPath = rootPath;
        options.Paths = paths;
        options.Database = database;
        options.Export = export;

        // Assert
        options.RootPath.Should().Be(rootPath);
        options.Paths.Should().Be(paths);
        options.Database.Should().Be(database);
        options.Export.Should().Be(export);
    }
}

[TestClass]
public class PathOptionsTests
{
    [TestMethod]
    public void PathOptions_DefaultValues_ShouldBeEmpty()
    {
        // Act
        var options = new PathOptions();

        // Assert
        options.Config.Should().BeEmpty();
        options.Data.Should().BeEmpty();
        options.Scripts.Should().BeEmpty();
    }

    [TestMethod]
    public void PathOptions_Properties_ShouldBeSettable()
    {
        // Arrange
        var options = new PathOptions();
        var configPath = @"C:\Config";
        var dataPath = @"C:\Data";
        var scriptsPath = @"C:\Scripts";

        // Act
        options.Config = configPath;
        options.Data = dataPath;
        options.Scripts = scriptsPath;

        // Assert
        options.Config.Should().Be(configPath);
        options.Data.Should().Be(dataPath);
        options.Scripts.Should().Be(scriptsPath);
    }
}

[TestClass]
public class DatabaseOptionsTests
{
    [TestMethod]
    public void DatabaseOptions_DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var options = new DatabaseOptions();

        // Assert
        options.DefaultName.Should().BeEmpty();
        options.Timeout.Should().Be(600);
    }

    [TestMethod]
    public void DatabaseOptions_Properties_ShouldBeSettable()
    {
        // Arrange
        var options = new DatabaseOptions();
        var defaultName = "TestDatabase";
        var timeout = 300;

        // Act
        options.DefaultName = defaultName;
        options.Timeout = timeout;

        // Assert
        options.DefaultName.Should().Be(defaultName);
        options.Timeout.Should().Be(timeout);
    }
}

[TestClass]
public class ExportOptionsTests
{
    [TestMethod]
    public void ExportOptions_DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var options = new ExportOptions();

        // Assert
        options.IncludeHeaders.Should().BeTrue();
        options.Delimiter.Should().Be(",");
        options.Encoding.Should().Be("UTF-8");
    }

    [TestMethod]
    public void ExportOptions_Properties_ShouldBeSettable()
    {
        // Arrange
        var options = new ExportOptions();
        var includeHeaders = false;
        var delimiter = ";";
        var encoding = "ASCII";

        // Act
        options.IncludeHeaders = includeHeaders;
        options.Delimiter = delimiter;
        options.Encoding = encoding;

        // Assert
        options.IncludeHeaders.Should().Be(includeHeaders);
        options.Delimiter.Should().Be(delimiter);
        options.Encoding.Should().Be(encoding);
    }
}
