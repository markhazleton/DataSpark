using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sql2Csv.Core.Models;
using FluentAssertions;

namespace Sql2Csv.Tests.Models;

[TestClass]
public class DatabaseConfigurationTests
{
    [TestMethod]
    public void DatabaseConfiguration_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        // Act
        var config = new DatabaseConfiguration();

        // Assert
        config.Name.Should().BeEmpty();
        config.ConnectionString.Should().BeEmpty();
        config.Type.Should().Be(DatabaseType.SQLite);
    }

    [TestMethod]
    public void DatabaseConfiguration_ParameterizedConstructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var name = "TestDB";
        var connectionString = "Data Source=test.db";
        var type = DatabaseType.SQLite;

        // Act
        var config = new DatabaseConfiguration(name, connectionString, type);

        // Assert
        config.Name.Should().Be(name);
        config.ConnectionString.Should().Be(connectionString);
        config.Type.Should().Be(type);
    }

    [TestMethod]
    public void DatabaseConfiguration_ParameterizedConstructor_WithDefaultType_ShouldUseSQLite()
    {
        // Arrange
        var name = "TestDB";
        var connectionString = "Data Source=test.db";

        // Act
        var config = new DatabaseConfiguration(name, connectionString);

        // Assert
        config.Name.Should().Be(name);
        config.ConnectionString.Should().Be(connectionString);
        config.Type.Should().Be(DatabaseType.SQLite);
    }

    [TestMethod]
    public void DatabaseConfiguration_ParameterizedConstructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Arrange
        string? name = null;
        var connectionString = "Data Source=test.db";

        // Act & Assert
        var action = () => new DatabaseConfiguration(name!, connectionString);
        action.Should().Throw<ArgumentNullException>().WithParameterName("name");
    }

    [TestMethod]
    public void DatabaseConfiguration_ParameterizedConstructor_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // Arrange
        var name = "TestDB";
        string? connectionString = null;

        // Act & Assert
        var action = () => new DatabaseConfiguration(name, connectionString!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("connectionString");
    }

    [TestMethod]
    public void FromSqliteFile_WithValidFilePath_ShouldCreateCorrectConfiguration()
    {
        // Arrange
        var filePath = @"C:\Databases\sample.db";

        // Act
        var config = DatabaseConfiguration.FromSqliteFile(filePath);

        // Assert
        config.Name.Should().Be("sample");
        config.ConnectionString.Should().Be($"Data Source={filePath}");
        config.Type.Should().Be(DatabaseType.SQLite);
    }

    [TestMethod]
    public void FromSqliteFile_WithFilePathWithoutExtension_ShouldUseFullName()
    {
        // Arrange
        var filePath = @"C:\Databases\sample";

        // Act
        var config = DatabaseConfiguration.FromSqliteFile(filePath);

        // Assert
        config.Name.Should().Be("sample");
        config.ConnectionString.Should().Be($"Data Source={filePath}");
        config.Type.Should().Be(DatabaseType.SQLite);
    }

    [TestMethod]
    public void FromSqliteFile_WithNullFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        string? filePath = null;

        // Act & Assert
        var action = () => DatabaseConfiguration.FromSqliteFile(filePath!);
        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void FromSqliteFile_WithEmptyFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        var filePath = string.Empty;

        // Act & Assert
        var action = () => DatabaseConfiguration.FromSqliteFile(filePath);
        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void FromSqliteFile_WithWhitespaceFilePath_ShouldThrowArgumentException()
    {
        // Arrange
        var filePath = "   ";

        // Act & Assert
        var action = () => DatabaseConfiguration.FromSqliteFile(filePath);
        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var config = new DatabaseConfiguration();
        var name = "NewName";
        var connectionString = "New Connection String";
        var type = DatabaseType.SqlServer;

        // Act
        config.Name = name;
        config.ConnectionString = connectionString;
        config.Type = type;

        // Assert
        config.Name.Should().Be(name);
        config.ConnectionString.Should().Be(connectionString);
        config.Type.Should().Be(type);
    }
}

[TestClass]
public class DatabaseTypeTests
{
    [TestMethod]
    public void DatabaseType_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)DatabaseType.SQLite).Should().Be(0);
        ((int)DatabaseType.SqlServer).Should().Be(1);
    }

    [TestMethod]
    public void DatabaseType_ShouldHaveCorrectNames()
    {
        // Assert
        DatabaseType.SQLite.ToString().Should().Be("SQLite");
        DatabaseType.SqlServer.ToString().Should().Be("SqlServer");
    }
}
