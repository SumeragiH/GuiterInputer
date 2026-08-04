using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuitarInputController.Core.Constants;
using GuitarInputController.Core.Enums;
using GuitarInputController.Core.Models;

namespace GuitarInputController.App.ViewModels;

/// <summary>
/// ViewModel for the add/edit mapping dialog.
/// Supports both creating new mappings and editing existing ones.
/// </summary>
public partial class MappingEditorViewModel : ObservableObject
{
    // ─────────────────────────────────────────────────────────
    //  Properties
    // ─────────────────────────────────────────────────────────

    /// <summary>Whether we are editing an existing mapping (true) or creating a new one (false).</summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>The ID of the mapping being edited (null for new).</summary>
    private string? _editingId;

    [ObservableProperty]
    private string _note = "C4";

    [ObservableProperty]
    private string _keyCode = "W";

    [ObservableProperty]
    private ObservableCollection<string> _modifierKeys = new();

    [ObservableProperty]
    private string? _label;

    // ── ComboBox index bindings ──────────────────────────────────

    [ObservableProperty]
    private int _actionTypeIndex = 0;
    public ActionType ActionType => _actionTypeIndex switch { 1 => ActionType.Combination, 2 => ActionType.MouseClick, _ => ActionType.KeyPress };

    [ObservableProperty]
    private int _triggerModeIndex = 0;
    public TriggerMode TriggerMode => _triggerModeIndex == 1 ? TriggerMode.Pulse : TriggerMode.Hold;

    [ObservableProperty]
    private int _mouseButtonIndex = 0;
    public MouseButtonType? MouseButton => _mouseButtonIndex switch { 1 => MouseButtonType.Right, 2 => MouseButtonType.Middle, _ => MouseButtonType.Left };

    [ObservableProperty]
    private string _windowTitle = "添加映射";

    /// <summary>
    /// After a successful Save, holds the built mapping so the view can retrieve it.
    /// Null if the dialog was cancelled.
    /// </summary>
    public NoteMapping? Result { get; private set; }

    /// <summary>
    /// Delegate invoked after Save or Cancel to close the dialog from the view layer.
    /// </summary>
    public Action? CloseAction { get; set; }

    // ─────────────────────────────────────────────────────────
    //  Dropdown data sources
    // ─────────────────────────────────────────────────────────

    /// <summary>Available note names for the dropdown.</summary>
    public static ObservableCollection<string> AvailableNotes { get; }

    /// <summary>Available key codes for the dropdown.</summary>
    public static ObservableCollection<string> AvailableKeys { get; }

    /// <summary>Available modifier keys for multi-select.</summary>
    public static ObservableCollection<string> AvailableModifiers { get; } = new()
    {
        "Ctrl", "Alt", "Shift", "Win"
    };

    /// <summary>Available action type display names for ComboBox.</summary>
    public static ObservableCollection<string> ActionTypeNames { get; } = new() { "按键", "组合键", "鼠标点击" };

    /// <summary>Available trigger mode display names for ComboBox.</summary>
    public static ObservableCollection<string> TriggerModeNames { get; } = new() { "按住模式", "脉冲模式" };

    /// <summary>Available mouse button display names for ComboBox.</summary>
    public static ObservableCollection<string> MouseButtonNames { get; } = new() { "左键", "右键", "中键" };

    // ─────────────────────────────────────────────────────────
    //  Visibility helpers for the view
    // ─────────────────────────────────────────────────────────

    /// <summary>Whether key code input is visible (KeyPress and Combination modes).</summary>
    public bool IsKeyCodeVisible => ActionType is ActionType.KeyPress or ActionType.Combination;

    /// <summary>Whether modifier keys selection is visible (Combination mode).</summary>
    public bool IsModifierVisible => ActionType == ActionType.Combination;

    /// <summary>Whether mouse button selection is visible (MouseClick mode).</summary>
    public bool IsMouseVisible => ActionType == ActionType.MouseClick;

    // ─────────────────────────────────────────────────────────
    //  Static constructor — precompute available data
    // ─────────────────────────────────────────────────────────

    static MappingEditorViewModel()
    {
        AvailableNotes = GenerateNoteList();
        AvailableKeys = new ObservableCollection<string>(GetCommonKeys());
    }

    private static ObservableCollection<string> GenerateNoteList()
    {
        var notes = new ObservableCollection<string>();
        for (int octave = 2; octave <= 6; octave++)
        {
            foreach (var name in NoteConstants.NoteNames)
            {
                notes.Add($"{name}{octave}");
            }
        }
        return notes;
    }

    private static List<string> GetCommonKeys() => new()
    {
        "W", "A", "S", "D", "Q", "E", "R", "F", "Z", "X", "C", "V",
        "Space", "Enter", "Escape", "Tab", "Backspace",
        "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
        "F1", "F2", "F3", "F4", "F5", "F6",
        "Left", "Right", "Up", "Down",
        "Shift", "Ctrl", "Alt"
    };

    // ─────────────────────────────────────────────────────────
    //  Initialization
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the ViewModel for creating a new mapping or editing an existing one.
    /// Pass an existing NoteMapping to enter edit mode.
    /// </summary>
    public MappingEditorViewModel(NoteMapping? existing = null)
    {
        if (existing != null)
        {
            InitializeForEdit(existing);
        }
        else
        {
            InitializeForNew();
        }
    }

    /// <summary>
    /// Initialises the ViewModel for creating a new mapping.
    /// </summary>
    public void InitializeForNew()
    {
        IsEditing = false;
        _editingId = null;
        WindowTitle = "添加映射";
        Note = "C4";
        ActionTypeIndex = 0;
        KeyCode = "W";
        ModifierKeys = new ObservableCollection<string>();
        TriggerModeIndex = 0;
        MouseButtonIndex = 0;
        Label = null;
        Result = null;
    }

    /// <summary>
    /// Initialises the ViewModel for editing an existing mapping.
    /// </summary>
    public void InitializeForEdit(NoteMapping mapping)
    {
        IsEditing = true;
        _editingId = mapping.Id;
        WindowTitle = "编辑映射";
        Note = mapping.Note;
        ActionTypeIndex = mapping.ActionType switch { ActionType.Combination => 1, ActionType.MouseClick => 2, _ => 0 };
        KeyCode = mapping.KeyCode ?? "W";
        ModifierKeys = new ObservableCollection<string>(mapping.ModifierKeys);
        TriggerModeIndex = mapping.TriggerMode == TriggerMode.Pulse ? 1 : 0;
        MouseButtonIndex = mapping.MouseButton switch { MouseButtonType.Right => 1, MouseButtonType.Middle => 2, _ => 0 };
        Label = mapping.Label;
        Result = null;
        OnPropertyChanged(nameof(IsKeyCodeVisible));
        OnPropertyChanged(nameof(IsModifierVisible));
        OnPropertyChanged(nameof(IsMouseVisible));
    }

    // ─────────────────────────────────────────────────────────
    //  Property change overrides for visibility
    // ─────────────────────────────────────────────────────────

    partial void OnActionTypeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsKeyCodeVisible));
        OnPropertyChanged(nameof(IsModifierVisible));
        OnPropertyChanged(nameof(IsMouseVisible));
    }

    // ─────────────────────────────────────────────────────────
    //  Commands
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the form and builds the resulting NoteMapping, then closes the dialog.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        var error = Validate();
        if (error != null)
        {
            // The view layer displays validation errors. We still set Result = null
            // so the view knows not to proceed with closing.
            Result = null;
            return;
        }

        Result = BuildMapping();
        CloseAction?.Invoke();
    }

    /// <summary>
    /// Closes the dialog without saving.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Result = null;
        CloseAction?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the current form data and returns an error message, or null if valid.
    /// </summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Note))
            return "请选择一个音符";

        if (ActionType is ActionType.KeyPress or ActionType.Combination)
        {
            if (string.IsNullOrWhiteSpace(KeyCode))
                return "请输入按键代码";

            if (ActionType == ActionType.Combination && ModifierKeys.Count == 0)
                return "组合键模式需要至少选择一个修饰键";
        }

        if (ActionType == ActionType.MouseClick && MouseButton == null)
            return "请选择鼠标按键";

        if (!AvailableNotes.Contains(Note))
            return $"无效的音符: {Note}";

        return null; // valid
    }

    /// <summary>
    /// Builds and returns the resulting NoteMapping from the current form state.
    /// Call only after Validate() returns null.
    /// </summary>
    public NoteMapping BuildMapping()
    {
        return new NoteMapping
        {
            Id = _editingId ?? Guid.NewGuid().ToString("N")[..8],
            Note = Note,
            ActionType = ActionType,
            KeyCode = ActionType is ActionType.KeyPress or ActionType.Combination ? KeyCode : null,
            ModifierKeys = ActionType == ActionType.Combination ? ModifierKeys.ToList() : new List<string>(),
            TriggerMode = TriggerMode,
            MouseButton = ActionType == ActionType.MouseClick ? MouseButton : null,
            Label = string.IsNullOrWhiteSpace(Label) ? null : Label
        };
    }

    /// <summary>
    /// Toggles a modifier key in or out of the ModifierKeys collection.
    /// </summary>
    public void ToggleModifier(string modifier)
    {
        if (ModifierKeys.Contains(modifier))
            ModifierKeys.Remove(modifier);
        else
            ModifierKeys.Add(modifier);
    }
}
