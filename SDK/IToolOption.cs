using System;
using System.Collections.Generic;

namespace Nox.Editor.Panel {
	/// <summary>
	/// Represents a toolbar button option that an IInstance can expose in the panel's options area.
	/// </summary>
	public interface IToolOption {
		/// <summary>
		/// The label displayed on the toolbar button.
		/// </summary>
		public string Label { get; }

		/// <summary>
		/// Optional tooltip shown on hover.
		/// </summary>
		public string Tooltip { get; }

		/// <summary>
		/// Whether this option is currently in an active/toggled state.
		/// </summary>
		public bool IsActive { get; }

		/// <summary>
		/// Called when the button is clicked.
		/// </summary>
		public void OnClick();
	}

	/// <summary>
	/// Default concrete implementation of <see cref="IToolOption"/>.
	/// </summary>
	public class DefaultToolOption : IToolOption {
		public string Label    { get; }
		public string Tooltip  { get; }
		public bool   IsActive { get; }

		private readonly Action _onClick;

		public DefaultToolOption(string label, Action onClick, string tooltip = null, bool isActive = false) {
			Label    = label;
			_onClick = onClick;
			Tooltip  = tooltip;
			IsActive = isActive;
		}

		public void OnClick() => _onClick?.Invoke();
	}

	/// <summary>
	/// A toolbar dropdown option. Renders as a ToolbarMenu with selectable choices.
	/// </summary>
	public class DropdownToolOption : IToolOption {
		public string        Label    { get; }
		public string        Tooltip  { get; }
		public bool          IsActive => false;
		public List<string>  Choices  { get; }
		public string        Value    { get; private set; }

		private readonly Action<string> _onChange;

		public DropdownToolOption(string label, List<string> choices, string initial, Action<string> onChange, string tooltip = null) {
			Label     = label;
			Choices   = choices;
			Value     = initial;
			_onChange = onChange;
			Tooltip   = tooltip;
		}

		public void OnClick() { }

		public void Select(string value) {
			Value = value;
			_onChange?.Invoke(value);
		}
	}

	/// <summary>
	/// A toolbar search-bar option. Renders as a ToolbarSearchField.
	/// </summary>
	public class InputToolOption : IToolOption {
		public string Label    { get; }
		public string Tooltip  { get; }
		public bool   IsActive => false;
		public string Value    { get; private set; }

		private readonly Action<string> _onChange;

		public InputToolOption(string label, Action<string> onChange, string tooltip = null) {
			Label     = label;
			_onChange = onChange;
			Tooltip   = tooltip;
			Value     = string.Empty;
		}

		public void OnClick() { }

		public void SetValue(string value) {
			Value = value;
			_onChange?.Invoke(value);
		}
	}
}
