namespace TQG.Automation.SDK.Shared;

/// <summary>
/// Ánh xạ địa chỉ bộ nhớ cho các tín hiệu thanh ghi PLC.
/// </summary>
public sealed class SignalMap
{
    #region --- Commands ---

    /// <summary>
    /// Tín hiệu lệnh nhập kho.
    /// Variable: Req_ImportPallet | Type: Bool
    /// </summary>
    public required string InboundCommand { get; init; }

    /// <summary>
    /// Tín hiệu lệnh xuất kho.
    /// Variable: Req_ExportPallet | Type: Bool
    /// </summary>
    public required string OutboundCommand { get; init; }

    /// <summary>
    /// Tín hiệu lệnh chuyển kho.
    /// Variable: Req_TransferPallet | Type: Bool
    /// </summary>
    public required string TransferCommand { get; init; }

    /// <summary>
    /// Tín hiệu lệnh bắt đầu xử lý.
    /// Variable: Req_StartProcess | Type: Bool
    /// </summary>
    public required string StartProcessCommand { get; init; }

    #endregion

    #region --- Status ---

    /// <summary>
    /// Trạng thái thiết bị sẵn sàng.
    /// Variable: Device_Ready | Type: Bool
    /// </summary>
    public required string DeviceReady { get; init; }

    /// <summary>
    /// Trạng thái kết nối với phần mềm.
    /// Variable: Connected_To_Software | Type: Bool
    /// </summary>
    public required string ConnectedToSoftware { get; init; }

    /// <summary>
    /// Trạng thái lệnh đã được xác nhận.
    /// Variable: Status_ShuttleBusy | Type: Bool
    /// </summary>
    public required string CommandAcknowledged { get; init; }

    /// <summary>
    /// Trạng thái lệnh bị từ chối.
    /// Variable: Status_InvalidPosition | Type: Bool
    /// </summary>
    public required string CommandRejected { get; init; }

    /// <summary>
    /// Trạng thái hoàn thành quy trình nhập kho.
    /// Variable: Done_ImportProcess | Type: Bool
    /// </summary>
    public required string InboundComplete { get; init; }

    /// <summary>
    /// Trạng thái hoàn thành quy trình xuất kho.
    /// Variable: Done_ExportProcess | Type: Bool
    /// </summary>
    public required string OutboundComplete { get; init; }

    /// <summary>
    /// Trạng thái hoàn thành quy trình chuyển kho.
    /// Variable: Done_TransferProcess | Type: Bool
    /// </summary>
    public required string TransferComplete { get; init; }

    /// <summary>
    /// Trạng thái lỗi trong quá trình thực thi lệnh.
    /// Variable: Error_Running | Type: Bool
    /// </summary>
    public required string Alarm { get; init; }

    /// <summary>
    /// Trạng thái lệnh bị hủy.
    /// Type Variable: Req_CancelCommand | Type: Bool
    /// </summary>
    public required string CancelCommand { get; init; }

    #endregion

    #region --- Input/Output Gate & Floor 3 Direction ---

    /// <summary>
    /// Hướng ra.
    /// True: Ra từ trên (Direction = Top), False: Ra từ dưới (Direction = Bottom)
    /// Variable: Dir_Src_Block3 | Type: Bool
    /// </summary>
    public required string OutDirBlock { get; init; }

    /// <summary>
    /// Hướng vào.
    /// True: Vào từ trên (Direction = Top), False: Vào từ dưới (Direction = Bottom)
    /// Variable: Dir_Taget_Block3 | Type: Bool
    /// </summary>
    public required string InDirBlock { get; init; }

    /// <summary>
    /// Số cổng vào/ra.
    /// Variable: Port_IO_Number | Type: Int
    /// </summary>
    public required string GateNumber { get; init; }

    #endregion

    #region --- Source Location Data ---

    /// <summary>
    /// Tầng nguồn.
    /// Variable: Source_Floor | Type: Int
    /// </summary>
    public required string SourceFloor { get; init; }

    /// <summary>
    /// Dãy nguồn.
    /// Variable: Source_Rail | Type: Int
    /// </summary>
    public required string SourceRail { get; init; }

    /// <summary>
    /// Khối nguồn.
    /// Variable: Source_Block | Type: Int
    /// </summary>
    public required string SourceBlock { get; init; }

    #endregion

    #region --- Target Location Data ---

    /// <summary>
    /// Tầng đích.
    /// Variable: Target_Floor | Type: Int
    /// </summary>
    public required string TargetFloor { get; init; }

    /// <summary>
    /// Dãy đích.
    /// Variable: Target_Rail | Type: Int
    /// </summary>
    public required string TargetRail { get; init; }

    /// <summary>
    /// Khối đích.
    /// Variable: Target_Block | Type: Int
    /// </summary>
    public required string TargetBlock { get; init; }

    #endregion

    #region --- Feedback ---

    /// <summary>
    /// Trạng thái barcode hợp lệ.
    /// Variable: Barcode_Valid | Type: Bool
    /// </summary>
    public required string BarcodeValid { get; init; }

    /// <summary>
    /// Trạng thái barcode không hợp lệ.
    /// Variable: Barcode_Invalid | Type: Bool
    /// </summary>
    public required string BarcodeInvalid { get; init; }

    /// <summary>
    /// Tầng thực tế.
    /// Variable: Cur_Shuttle_Floor | Type: Int
    /// </summary>
    public required string ActualFloor { get; init; }

    /// <summary>
    /// Dãy thực tế.
    /// Variable: Cur_Shuttle_Rail | Type: Int
    /// </summary>
    public required string ActualRail { get; init; }

    /// <summary>
    /// Khối thực tế.
    /// Variable: Cur_Shuttle_Block | Type: Int
    /// </summary>
    public required string ActualBlock { get; init; }

    /// <summary>
    /// Vị trí sâu thực tế.
    /// Variable: Cur_Shuttle_Depth | Type: Int
    /// </summary>
    public required string ActualDepth { get; init; }

    /// <summary>
    /// Mã lỗi.
    /// Variable: System_ErrorCode | Type: Int
    /// </summary>
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Ký tự thứ 1 của barcode pallet.
    /// Variable: Barcode | Type: Int
    /// </summary>
    public required string BarcodeChar1 { get; init; }

    /// <summary>
    /// Ký tự thứ 2 của barcode pallet.
    /// Variable: Barcode | Type: Int
    /// </summary>
    public required string BarcodeChar2 { get; init; }

    /// <summary>
    /// Ký tự thứ 3 của barcode pallet.
    /// Variable: Barcode | Type: Int
    /// </summary>
    public required string BarcodeChar3 { get; init; }

    /// <summary>
    /// Ký tự thứ 4 của barcode pallet.
    /// Variable: Barcode | Type: Int
    /// </summary>
    public required string BarcodeChar4 { get; init; }

    /// <summary>
    /// Ký tự thứ 5 của barcode pallet.
    /// Variable: Barcode | Type: Int
    /// </summary>
    public required string BarcodeChar5 { get; init; }

    /// <summary>
    /// Ký tự thứ 6 của barcode pallet.
    /// Variable: Barcode | Type: Int
    /// </summary>
    public required string BarcodeChar6 { get; init; }

    /// <summary>
    /// Ký tự thứ 7 của barcode pallet.
    /// Variable: Barcode | Type: Int
    /// </summary>
    public required string BarcodeChar7 { get; init; }

    /// <summary>
    /// Ký tự thứ 8 của barcode pallet.
    /// Variable: Barcode | Type: Int
    /// </summary>
    public required string BarcodeChar8 { get; init; }

    /// <summary>
    /// Ký tự thứ 9 của barcode pallet.
    /// Variable: Barcode | Type: Int
    /// </summary>
    public required string BarcodeChar9 { get; init; }

    /// <summary>
    /// Ký tự thứ 10 của barcode pallet.
    /// Variable: Barcode | Type: Int
    /// </summary>
    public required string BarcodeChar10 { get; init; }

    #endregion
}
