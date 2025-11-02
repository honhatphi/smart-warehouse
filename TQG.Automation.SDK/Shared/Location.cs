namespace TQG.Automation.SDK.Shared;

/// <summary>
/// Đại diện cho một vị trí trong hệ thống kệ lưu trữ.
/// </summary>
/// <param name="Floor">Tầng</param>
/// <param name="Rail">Dãy</param>
/// <param name="Block">Kệ</param>
public record Location(short Floor, short Rail, short Block, short Depth = 0);
