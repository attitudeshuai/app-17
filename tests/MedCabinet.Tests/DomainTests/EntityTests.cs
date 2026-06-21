using FluentAssertions;
using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Enums;

namespace MedCabinet.Tests.DomainTests;

public class MedicineEntityTests
{
    [Fact]
    public void Medicine_Should_Have_DefaultValues()
    {
        // Arrange & Act
        var medicine = new Medicine();

        // Assert
        medicine.Name.Should().Be(string.Empty);
        medicine.Category.Should().Be(string.Empty);
        medicine.StockQuantity.Should().Be(0);
        medicine.Status.Should().Be(MedicineStatus.Valid);
        medicine.MedUsages.Should().NotBeNull();
        medicine.MedAlerts.Should().NotBeNull();
    }

    [Fact]
    public void Medicine_Should_Set_Properties_Correctly()
    {
        // Arrange & Act
        var medicine = new Medicine
        {
            Id = 1,
            HouseholdId = 1,
            Name = "布洛芬",
            Category = "解热镇痛",
            Indication = "用于缓解疼痛",
            Dosage = "一次1粒",
            ExpiryDate = new DateTime(2025, 12, 31),
            StockQuantity = 20,
            StorageLocation = "客厅药箱",
            Status = MedicineStatus.Valid
        };

        // Assert
        medicine.Id.Should().Be(1);
        medicine.Name.Should().Be("布洛芬");
        medicine.Category.Should().Be("解热镇痛");
        medicine.StockQuantity.Should().Be(20);
        medicine.Status.Should().Be(MedicineStatus.Valid);
    }
}

public class UserEntityTests
{
    [Fact]
    public void User_Should_Have_DefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Username.Should().Be(string.Empty);
        user.Email.Should().Be(string.Empty);
        user.PasswordHash.Should().Be(string.Empty);
        user.HouseholdMembers.Should().NotBeNull();
    }

    [Fact]
    public void User_Should_Set_Properties_Correctly()
    {
        // Arrange & Act
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashedpassword"
        };

        // Assert
        user.Username.Should().Be("testuser");
        user.Email.Should().Be("test@example.com");
    }
}

public class HouseholdEntityTests
{
    [Fact]
    public void Household_Should_Have_DefaultValues()
    {
        // Arrange & Act
        var household = new Household();

        // Assert
        household.Name.Should().Be(string.Empty);
        household.InviteCode.Should().Be(string.Empty);
        household.HouseholdMembers.Should().NotBeNull();
        household.Medicines.Should().NotBeNull();
    }

    [Fact]
    public void Household_Should_Set_Properties_Correctly()
    {
        // Arrange & Act
        var household = new Household
        {
            Id = 1,
            Name = "我的家庭",
            InviteCode = "ABC123",
            CreatedBy = 1
        };

        // Assert
        household.Name.Should().Be("我的家庭");
        household.InviteCode.Should().Be("ABC123");
        household.CreatedBy.Should().Be(1);
    }
}
