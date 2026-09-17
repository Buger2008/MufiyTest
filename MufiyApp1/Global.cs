// ============================================================================
// 全局类型别名（Global Type Aliases）
//
// 【为什么需要这个文件】
// Mufiy 与 WinForms / System.Drawing 存在大量**同名类型**（Button、Panel、Padding、
// Font、MouseEventArgs、DataGridViewRow……）。VS 设计器自动生成的代码使用裸类型名
// （例如 new Button()、new Padding(0)），而在 net10.0-windows 目标下，Mufiy 与
// WinForms 同时参与编译，这些名字默认会解析到 WinForms 的类型：
//   · 设计器里看到的控件类型与运行期不是同一个类型，属性设置会失败或行为不一致；
//   · 设计器生成的代码与手写代码互相赋值时报类型不匹配。
// 因此在这里统一把同名类型指向 Mufiy 版本。
//
// 【覆盖范围】
// 与 WinForms / System.Drawing 同名的：控件、基础类型与结构、枚举、事件参数、
// 绘图类型、对话框；（为让设计器生成的裸类型名也能解析到 Mufiy）。
//
// 【注意】
// Mufiy 控件用 CheckBoxState，设计器生成的菜单代码用 CheckState。
// ============================================================================

// ---------------------------------------------------------------------------
// 布局与外观：枚举、结构
// ---------------------------------------------------------------------------
global using AnchorStyles = Mufiy.AnchorStyles;
// ---------------------------------------------------------------------------
// 数据绑定
// ---------------------------------------------------------------------------
global using Binding = Mufiy.Core.Binding.Binding;
// ---------------------------------------------------------------------------
// 绘图类型
// ---------------------------------------------------------------------------
global using Bitmap = Mufiy.Drawing.Bitmap;
global using BorderStyle = Mufiy.BorderStyle;
global using Brush = Mufiy.Drawing.Brush;
// ---------------------------------------------------------------------------
// 标准控件
// ---------------------------------------------------------------------------
global using Button = Mufiy.Controls.Button;
global using Carousel = Mufiy.Controls.Carousel;
global using ChartControl = Mufiy.Controls.Chart.Chart;
global using CheckBox = Mufiy.Controls.CheckBox;
global using CheckedListBox = Mufiy.Controls.CheckedListBox;
// ---------------------------------------------------------------------------
// 基础类型：控件基类、容器、集合与剪贴板
// ---------------------------------------------------------------------------
global using Clipboard = Mufiy.Clipboard;
global using ColorPicker = Mufiy.Controls.ColorPicker;
global using ComboBox = Mufiy.Controls.ComboBox;
// ---------------------------------------------------------------------------
// 对话框
// ---------------------------------------------------------------------------
global using CommonDialog = Mufiy.Dialogs.CommonDialog;
global using ContainerControl = Mufiy.ContainerControl;
global using ContentAlignment = Mufiy.ContentAlignment;
global using ContextMenuStrip = Mufiy.Controls.ContextMenuStrip;
global using Control = Mufiy.Control;
global using ControlCollection = Mufiy.ControlCollection;
global using DashStyle = Mufiy.DashStyle;
global using DataGridView = Mufiy.Controls.DataGridView;
// ---------------------------------------------------------------------------
// DataGridView 配套类型
// ---------------------------------------------------------------------------
global using DataGridViewButtonColumn = Mufiy.DataGridViewButtonColumn;
global using DataGridViewCell = Mufiy.DataGridViewCell;
global using DataGridViewCellCollection = Mufiy.DataGridViewCellCollection;
// ---------------------------------------------------------------------------
// 事件参数
// ---------------------------------------------------------------------------
global using DataGridViewCellEventArgs = Mufiy.Events.DataGridViewCellEventArgs;
global using DataGridViewCellFormattingEventArgs = Mufiy.Events.DataGridViewCellFormattingEventArgs;
global using DataGridViewCellMouseEventArgs = Mufiy.Events.DataGridViewCellMouseEventArgs;
global using DataGridViewCellParsingEventArgs = Mufiy.Events.DataGridViewCellParsingEventArgs;
global using DataGridViewCellStyle = Mufiy.DataGridViewCellStyle;
global using DataGridViewCellValidatingEventArgs = Mufiy.Events.DataGridViewCellValidatingEventArgs;
global using DataGridViewCellValueEventArgs = Mufiy.Events.DataGridViewCellValueEventArgs;
global using DataGridViewCheckBoxColumn = Mufiy.DataGridViewCheckBoxColumn;
global using DataGridViewColumn = Mufiy.DataGridViewColumn;
global using DataGridViewColumnCollection = Mufiy.DataGridViewColumnCollection;
global using DataGridViewColumnEventArgs = Mufiy.Controls.DataGridViewColumnEventArgs;
global using DataGridViewColumnSortMode = Mufiy.DataGridViewColumnSortMode;
global using DataGridViewComboBoxCell = Mufiy.DataGridViewComboBoxCell;
global using DataGridViewComboBoxColumn = Mufiy.DataGridViewComboBoxColumn;
global using DataGridViewEditMode = Mufiy.DataGridViewEditMode;
global using DataGridViewImageCell = Mufiy.DataGridViewImageCell;
global using DataGridViewImageColumn = Mufiy.DataGridViewImageColumn;
global using DataGridViewLinkCell = Mufiy.DataGridViewLinkCell;
global using DataGridViewLinkColumn = Mufiy.DataGridViewLinkColumn;
global using DataGridViewRow = Mufiy.DataGridViewRow;
global using DataGridViewRowCollection = Mufiy.DataGridViewRowCollection;
global using DataGridViewRowEventArgs = Mufiy.Controls.DataGridViewRowEventArgs;
global using DataGridViewSelectionMode = Mufiy.DataGridViewSelectionMode;
global using DataGridViewTextBoxColumn = Mufiy.DataGridViewTextBoxColumn;
global using DataGridViewTopLeftHeaderCell = Mufiy.DataGridViewTopLeftHeaderCell;
global using DateTimePicker = Mufiy.Controls.DateTimePicker;
global using DateTimePickerFormat = Mufiy.Controls.DateTimePickerFormat;
global using DialogResult = Mufiy.DialogResult;
global using DockStyle = Mufiy.DockStyle;
global using FixedPanel = Mufiy.FixedPanel;
global using FlowDirection = Mufiy.FlowDirection;
global using FlowLayoutPanel = Mufiy.Controls.FlowLayoutPanel;
global using FolderBrowserDialog = Mufiy.Dialogs.FolderBrowserDialog;
global using Font = Mufiy.Drawing.Font;
global using Graphics = Mufiy.Graphics;
global using GraphicsPath = Mufiy.Drawing.GraphicsPath;
global using GroupBox = Mufiy.Controls.GroupBox;
global using HorizontalAlignment = Mufiy.HorizontalAlignment;
global using IContainerControl = Mufiy.IContainerControl;
global using Image = Mufiy.Drawing.Image;
global using ImageFormat = Mufiy.ImageFormat;
global using ImageLayout = Mufiy.ImageLayout;
global using ImageList = Mufiy.Controls.ImageList;
global using InputCharEventArgs = Mufiy.Events.InputCharEventArgs;
global using KeyboardEventArgs = Mufiy.Events.KeyboardEventArgs;
global using Label = Mufiy.Controls.Label;
global using LinearGradientBrush = Mufiy.Drawing.LinearGradientBrush;
global using LinkLabel = Mufiy.Controls.LinkLabel;
global using ListBox = Mufiy.Controls.ListBox;
global using ListView = Mufiy.Controls.ListView;
global using MenuItemCollection = Mufiy.MenuItemCollection;
global using MenuStrip = Mufiy.Controls.MenuStrip;
global using MessageBox = Mufiy.Dialogs.MessageBox;
global using MessageBoxButtons = Mufiy.MessageBoxButtons;
global using MessageBoxIcon = Mufiy.MessageBoxIcon;
global using MonthCalendar = Mufiy.Controls.MonthCalendar;
global using MouseEventArgs = Mufiy.Events.MouseEventArgs;
global using MouseScrollWheelEventArgs = Mufiy.Events.MouseScrollWheelEventArgs;
global using NotifyIcon = Mufiy.Controls.NotifyIcon;
global using NumericUpDown = Mufiy.Controls.NumericUpDown;
global using OpenFileDialog = Mufiy.Dialogs.OpenFileDialog;
global using Orientation = Mufiy.Orientation;
global using Padding = Mufiy.Padding;
global using Panel = Mufiy.Controls.Panel;
global using PathGradientBrush = Mufiy.Drawing.PathGradientBrush;
global using Pen = Mufiy.Drawing.Pen;
global using PictureBox = Mufiy.Controls.PictureBox;
global using ProgressBar = Mufiy.Controls.ProgressBar;
global using RadioButton = Mufiy.Controls.RadioButton;
global using RichTextBox = Mufiy.Controls.RichTextBox;
global using RightToLeft = Mufiy.RightToLeft;
global using SaveFileDialog = Mufiy.Dialogs.SaveFileDialog;
global using Screen = Mufiy.Screen;
global using ScrollableControl = Mufiy.Controls.ScrollableControl;
global using ScrollBar = Mufiy.Controls.ScrollBar;
global using ScrollBars = Mufiy.ScrollBars;
global using ScrollEventArgs = Mufiy.Events.ScrollEventArgs;
global using ScrollEventType = Mufiy.ScrollEventType;
global using Shadow = Mufiy.Drawing.Shadow;
global using SolidBrush = Mufiy.Drawing.SolidBrush;
global using SplitContainer = Mufiy.Controls.SplitContainer;
global using SplitterPanel = Mufiy.Controls.SplitterPanel;
global using StringAlignment = Mufiy.StringAlignment;
global using TabControl = Mufiy.Controls.TabControl;
global using TableLayoutPanel = Mufiy.Controls.TableLayoutPanel;
global using TabPage = Mufiy.Controls.TabPage;
global using TabPageCollection = Mufiy.TabPageCollection;
global using TextBox = Mufiy.Controls.TextBox;
global using Timer = Mufiy.Controls.Timer;
global using ToggleSwitch = Mufiy.Controls.ToggleSwitch;
global using ToolStrip = Mufiy.Controls.ToolStrip;
global using TrackBar = Mufiy.Controls.TrackBar;
global using TreeNode = Mufiy.TreeNode;
global using TreeNodeCollection = Mufiy.TreeNodeCollection;
global using TreeView = Mufiy.Controls.TreeView;
global using UserControl = Mufiy.Controls.UserControl;
global using VerticalAlignment = Mufiy.VerticalAlignment;
global using Window = Mufiy.Window;
global using WrapMode = Mufiy.WrapMode;
