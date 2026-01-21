using Subrom.Domain.Aggregates.Storage;
using DriveType = Subrom.Domain.Aggregates.Storage.DriveType;

namespace Subrom.Tests.Unit.Domain;

public class DriveTests {
	[Fact]
	public void Create_WithLocalPath_CreatesFixedDrive() {
		// Act
		var drive = Drive.Create("Local", @"C:\ROMs", DriveType.Fixed);

		// Assert
		Assert.Equal("Local", drive.Label);
		Assert.Equal(@"C:\ROMs", drive.RootPath);
		Assert.Equal(DriveType.Fixed, drive.DriveType);
		Assert.False(drive.IsNetworkPath);
	}

	[Fact]
	public void Create_WithUncPath_AutoDetectsNetworkDrive() {
		// Act
		var drive = Drive.Create("NAS", @"\\server\share", DriveType.Fixed);

		// Assert
		Assert.Equal(DriveType.Network, drive.DriveType);
		Assert.True(drive.IsNetworkPath);
	}

	[Fact]
	public void CreateNetworkDrive_WithValidUncPath_Creates() {
		// Act
		var drive = Drive.CreateNetworkDrive("NAS", @"\\server\share\roms");

		// Assert
		Assert.Equal("NAS", drive.Label);
		Assert.Equal(@"\\server\share\roms", drive.RootPath);
		Assert.Equal(DriveType.Network, drive.DriveType);
		Assert.True(drive.IsNetworkPath);
	}

	[Fact]
	public void CreateNetworkDrive_WithInvalidPath_Throws() {
		// Act & Assert
		Assert.Throws<ArgumentException>(() =>
			Drive.CreateNetworkDrive("Bad", @"C:\notnetwork"));
	}

	[Fact]
	public void Create_NormalizesPath_RemovesTrailingSlash() {
		// Act
		var drive1 = Drive.Create("Test1", @"C:\ROMs\");
		var drive2 = Drive.Create("Test2", @"C:\ROMs");

		// Assert
		Assert.Equal(@"C:\ROMs", drive1.RootPath);
		Assert.Equal(@"C:\ROMs", drive2.RootPath);
	}

	[Fact]
	public void MarkOnline_UpdatesStatusAndTime() {
		// Arrange
		var drive = Drive.Create("Test", @"C:\Test");
		var beforeTime = DateTime.UtcNow;

		// Act
		drive.MarkOnline(1000000000, 500000000);

		// Assert
		Assert.True(drive.IsOnline);
		Assert.True(drive.LastSeenAt >= beforeTime);
		Assert.Equal(1000000000, drive.TotalSize);
		Assert.Equal(500000000, drive.FreeSpace);
	}

	[Fact]
	public void MarkOffline_UpdatesStatus() {
		// Arrange
		var drive = Drive.Create("Test", @"C:\Test");
		drive.MarkOnline();

		// Act
		drive.MarkOffline();

		// Assert
		Assert.False(drive.IsOnline);
	}

	[Fact]
	public void GetFullPath_CombinesCorrectly() {
		// Arrange
		var drive = Drive.Create("Test", @"C:\ROMs");

		// Act
		var fullPath = drive.GetFullPath(@"Nintendo\SNES\game.sfc");

		// Assert
		Assert.Equal(@"C:\ROMs\Nintendo\SNES\game.sfc", fullPath);
	}

	[Fact]
	public void GetFullPath_ForNetworkDrive_CombinesCorrectly() {
		// Arrange
		var drive = Drive.CreateNetworkDrive("NAS", @"\\server\share");

		// Act
		var fullPath = drive.GetFullPath(@"ROMs\SNES\game.sfc");

		// Assert
		Assert.Equal(@"\\server\share\ROMs\SNES\game.sfc", fullPath);
	}

	[Fact]
	public void Create_AddsRegisteredEvent() {
		// Act
		var drive = Drive.Create("Test", @"C:\Test");

		// Assert
		var events = drive.DomainEvents;
		Assert.Contains(events, e => e is DriveRegisteredEvent);
	}

	[Fact]
	public void MarkOnline_AddsOnlineEvent() {
		// Arrange
		var drive = Drive.Create("Test", @"C:\Test");
		drive.ClearDomainEvents();

		// Act
		drive.MarkOnline();

		// Assert
		var events = drive.DomainEvents;
		Assert.Contains(events, e => e is DriveOnlineEvent);
	}

	[Fact]
	public void MarkOffline_AddsOfflineEvent() {
		// Arrange
		var drive = Drive.Create("Test", @"C:\Test");
		drive.MarkOnline();
		drive.ClearDomainEvents();

		// Act
		drive.MarkOffline();

		// Assert
		var events = drive.DomainEvents;
		Assert.Contains(events, e => e is DriveOfflineEvent);
	}

	[Fact]
	public void IsNetworkPath_ForFixedDrive_ReturnsFalse() {
		// Arrange
		var drive = Drive.Create("Local", @"C:\ROMs", DriveType.Fixed);

		// Assert
		Assert.False(drive.IsNetworkPath);
	}

	[Fact]
	public void IsNetworkPath_ForNetworkType_ReturnsTrue() {
		// Arrange
		var drive = Drive.Create("Mapped", @"Z:\ROMs", DriveType.Network);

		// Assert
		Assert.True(drive.IsNetworkPath);
	}
}
