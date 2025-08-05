using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sql2Csv.Core.Models;
using FluentAssertions;

namespace Sql2Csv.Tests.Models;

[TestClass]
public class ExportResultTests
{
    [TestMethod]
    public void ExportResult_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var databaseName = "TestDB";
        var tableName = "Users";
        var outputFilePath = @"C:\Output\users.csv";
        var rowCount = 100;
        var duration = TimeSpan.FromSeconds(5);
        var isSuccess = true;
        var errorMessage = "Test error";

        // Act
        var result = new ExportResult
        {
            DatabaseName = databaseName,
            TableName = tableName,
            OutputFilePath = outputFilePath,
            RowCount = rowCount,
            Duration = duration,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage
        };

        // Assert
        result.DatabaseName.Should().Be(databaseName);
        result.TableName.Should().Be(tableName);
        result.OutputFilePath.Should().Be(outputFilePath);
        result.RowCount.Should().Be(rowCount);
        result.Duration.Should().Be(duration);
        result.IsSuccess.Should().Be(isSuccess);
        result.ErrorMessage.Should().Be(errorMessage);
    }

    [TestMethod]
    public void ExportResult_SuccessfulExport_ShouldHaveNullErrorMessage()
    {
        // Act
        var result = new ExportResult
        {
            DatabaseName = "TestDB",
            TableName = "Users",
            OutputFilePath = @"C:\Output\users.csv",
            RowCount = 100,
            Duration = TimeSpan.FromSeconds(5),
            IsSuccess = true,
            ErrorMessage = null
        };

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [TestMethod]
    public void ExportResult_FailedExport_ShouldHaveErrorMessage()
    {
        // Act
        var result = new ExportResult
        {
            DatabaseName = "TestDB",
            TableName = "Users",
            OutputFilePath = @"C:\Output\users.csv",
            RowCount = 0,
            Duration = TimeSpan.FromSeconds(1),
            IsSuccess = false,
            ErrorMessage = "Database connection failed"
        };

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Database connection failed");
        result.RowCount.Should().Be(0);
    }
}

[TestClass]
public class ColumnInfoTests
{
    [TestMethod]
    public void ColumnInfo_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var name = "UserId";
        var dataType = "INTEGER";
        var isNullable = false;
        var isPrimaryKey = true;
        var defaultValue = "1";

        // Act
        var column = new ColumnInfo
        {
            Name = name,
            DataType = dataType,
            IsNullable = isNullable,
            IsPrimaryKey = isPrimaryKey,
            DefaultValue = defaultValue
        };

        // Assert
        column.Name.Should().Be(name);
        column.DataType.Should().Be(dataType);
        column.IsNullable.Should().Be(isNullable);
        column.IsPrimaryKey.Should().Be(isPrimaryKey);
        column.DefaultValue.Should().Be(defaultValue);
    }

    [TestMethod]
    public void ColumnInfo_WithNullDefaultValue_ShouldAllowNull()
    {
        // Act
        var column = new ColumnInfo
        {
            Name = "UserName",
            DataType = "TEXT",
            IsNullable = true,
            IsPrimaryKey = false,
            DefaultValue = null
        };

        // Assert
        column.DefaultValue.Should().BeNull();
        column.IsNullable.Should().BeTrue();
        column.IsPrimaryKey.Should().BeFalse();
    }

    [TestMethod]
    public void ColumnInfo_NonPrimaryKeyColumn_ShouldHaveCorrectFlags()
    {
        // Act
        var column = new ColumnInfo
        {
            Name = "Email",
            DataType = "TEXT",
            IsNullable = true,
            IsPrimaryKey = false
        };

        // Assert
        column.IsPrimaryKey.Should().BeFalse();
        column.IsNullable.Should().BeTrue();
    }
}

[TestClass]
public class TableInfoTests
{
    [TestMethod]
    public void TableInfo_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var name = "Users";
        var schema = "dbo";
        var columns = new List<ColumnInfo>
        {
            new() { Name = "Id", DataType = "INTEGER", IsPrimaryKey = true, IsNullable = false },
            new() { Name = "Name", DataType = "TEXT", IsPrimaryKey = false, IsNullable = false }
        }.AsReadOnly();
        var rowCount = 1000L;

        // Act
        var table = new TableInfo
        {
            Name = name,
            Schema = schema,
            Columns = columns,
            RowCount = rowCount
        };

        // Assert
        table.Name.Should().Be(name);
        table.Schema.Should().Be(schema);
        table.Columns.Should().HaveCount(2);
        table.Columns.Should().BeEquivalentTo(columns);
        table.RowCount.Should().Be(rowCount);
    }

    [TestMethod]
    public void TableInfo_WithDefaultSchema_ShouldUseMainSchema()
    {
        // Act
        var table = new TableInfo
        {
            Name = "Users"
        };

        // Assert
        table.Schema.Should().Be("main");
        table.Columns.Should().BeEmpty();
        table.RowCount.Should().Be(0);
    }

    [TestMethod]
    public void TableInfo_WithEmptyColumns_ShouldAllowEmptyList()
    {
        // Act
        var table = new TableInfo
        {
            Name = "EmptyTable",
            Schema = "test",
            Columns = new List<ColumnInfo>().AsReadOnly(),
            RowCount = 0
        };

        // Assert
        table.Columns.Should().BeEmpty();
        table.RowCount.Should().Be(0);
    }

    [TestMethod]
    public void TableInfo_WithLargeRowCount_ShouldHandleLongValues()
    {
        // Arrange
        var largeRowCount = long.MaxValue;

        // Act
        var table = new TableInfo
        {
            Name = "LargeTable",
            RowCount = largeRowCount
        };

        // Assert
        table.RowCount.Should().Be(largeRowCount);
    }

    [TestMethod]
    public void TableInfo_Columns_ShouldBeReadOnly()
    {
        // Arrange
        var columns = new List<ColumnInfo>
        {
            new() { Name = "Id", DataType = "INTEGER", IsPrimaryKey = true, IsNullable = false }
        };

        // Act
        var table = new TableInfo
        {
            Name = "TestTable",
            Columns = columns.AsReadOnly()
        };

        // Assert
        table.Columns.Should().BeAssignableTo<IReadOnlyList<ColumnInfo>>();
        table.Columns.Should().HaveCount(1);
    }
}
