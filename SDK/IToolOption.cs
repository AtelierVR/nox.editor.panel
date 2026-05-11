using System;

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
}
