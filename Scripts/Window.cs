using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Editor.Panel.Runtime {
	public class Window : UnityEditor.EditorWindow, IWindow {
		[SerializeField]
		private IInstance _active;

		[SerializeField]
		private ResourceIdentifier panelId;

		private Dictionary<string, object> _panelData;

		public IInstance GetActive() {
			if (Editor.CoreAPI == null) return null;
			return _active ??= PanelManager.TryGetPanel(panelId, out var panel)
				? panel.Instantiate(this, _panelData)
				: throw new InvalidOperationException($"No panel found for id '{panelId.ToString() ?? "null"}'");
		}

		public static Window Create() {
			var window = CreateInstance<Window>();
			window.maxSize = new Vector2(512, window.maxSize.y);
			return window;
		}

		public bool SetActive(IPanel panel, Dictionary<string, object> data = null) {
			try {
				var old = _active;
				_active = panel.Instantiate(this, data ?? new Dictionary<string, object>());
				old?.OnDestroy();
				panelId = new ResourceIdentifier(null, panel.GetPath());
				_panelData = data ?? new Dictionary<string, object>();
				UpdateMenu();
				UpdateContent();
			} catch (Exception e) {
				Editor.CoreAPI.LoggerAPI.LogException(new Exception($"Failed to instantiate panel '{panel.GetPath()}'", e));
				return false;
			}

			return true;
		}

		public void OnDestroy() {
			GetActive()?.OnDestroy();
			WindowManager.RemoveWindow(this);
			_active = null;
		}

		public new void Repaint() {
			base.Repaint();
			UpdateMenu();
			UpdateContent();
		}

		public void OnFocus() {
			GetActive()?.OnFocus();
			UpdateMenu();
			UpdateContent();
		}

		public new void Show() {
			base.Show();
			GetActive()?.OnFocus();
		}


		private ToolbarButton _menu;
		private ToolbarBreadcrumbs _breadcrumbs;
		private VisualElement _content;
		private VisualElement _options;

		public ToolbarButton Menu {
			get {
				if (_menu != null)
					return _menu;

				_menu = rootVisualElement.Q<ToolbarButton>("menu");
				_menu?.RegisterCallback<ClickEvent>(_ => OpenPanelMenu());

				return _menu;
			}
		}

		public ToolbarBreadcrumbs Breadcrumbs
			=> _breadcrumbs ??= rootVisualElement.Q<ToolbarBreadcrumbs>("navigation");

		public VisualElement Content
			=> _content ??= rootVisualElement.Q<VisualElement>("content");

		public VisualElement Options
			=> _options ??= rootVisualElement.Q<VisualElement>("options");

		private void UpdateMenu() {
			if (Menu != null) {
				var active = GetActive();
				Menu.text = "Menu";
				Menu.tooltip = active?.GetPanel().GetLabel() ?? "Switch panel";

				if (active != null)
					titleContent = new GUIContent($"{active.GetTitle()} - {Application.productName}");
			}

			if (Breadcrumbs != null) {
				while (Breadcrumbs.childCount > 0)
					Breadcrumbs.PopItem();

				var active = GetActive();
				if (active != null) {
					foreach (var item in active.GetPanel().GetLabel().Split('/'))
						Breadcrumbs.PushItem(item);
				}
			}

			UpdateOptions();
		}

		/// <summary>
		/// Last segment of a panel label: "Editor/Translations" -> "Translations". The full path is
		/// already shown by the breadcrumbs, so the toolbar button only needs the leaf name.
		/// </summary>
		private static string ShortLabel(string label) {
			if (string.IsNullOrEmpty(label))
				return label ?? string.Empty;

			var index = label.LastIndexOf('/');
			return index < 0 ? label : label.Substring(index + 1);
		}

		private void UpdateOptions() {
			if (Options == null) return;
			Options.Clear();
			var active = GetActive();
			if (active == null) return;
			foreach (var opt in active.GetOptions()) {
				if (opt is DropdownToolOption dropdown) {
					var button = new ToolbarButton { text = $"{opt.Label}: {dropdown.Value}" };
					if (!string.IsNullOrEmpty(opt.Tooltip)) button.tooltip = opt.Tooltip;
					button.RegisterCallback<ClickEvent>(_ => ShowDropdownPopup(button, dropdown));
					Options.Add(button);
				} else if (opt is InputToolOption search) {
					var field = new ToolbarSearchField();
					field.value = search.Value;
					field.AddToClassList("toolbar-input");
					if (!string.IsNullOrEmpty(opt.Tooltip)) field.tooltip = opt.Tooltip;
					field.RegisterValueChangedCallback(evt => search.SetValue(evt.newValue));
					Options.Add(field);
				} else {
					var btn = new ToolbarButton(opt.OnClick) { text = opt.Label };
					if (!string.IsNullOrEmpty(opt.Tooltip)) btn.tooltip = opt.Tooltip;
					if (opt.IsActive) btn.AddToClassList("unity-toolbar-button--active");
					Options.Add(btn);
				}
			}
		}

		/// <summary>
		/// Opens the panel switcher: a tree menu built like Unity's "Add Component" menu (see
		/// <see cref="TreeMenuWindow"/>), so nested panel labels such as "Editor/Logger" become submenus
		/// instead of a flat list.
		/// </summary>
		private void OpenPanelMenu() {
			var choices = new List<string>();
			foreach (var panel in PanelManager.GetPanels()) {
				if (!panel.IsVisible())
					continue;

				var label = panel.GetLabel();
				if (!choices.Contains(label))
					choices.Add(label);
			}

			if (choices.Count == 0)
				return;

			var active = GetActive();
			var menu = new TreeMenuWindow(
				new AdvancedDropdownState(),
				"Panels",
				choices,
				active?.GetPanel().GetLabel(),
				ActivatePanel
			);

			menu.Show(Menu.worldBound);
		}

		/// <summary>
		/// Opens a toolbar option dropdown as an "Add Component"-like menu (see <see cref="TreeMenuWindow"/>).
		/// Choices without a '/' render as a single level, '/'-separated ones become submenus.
		/// </summary>
		private void ShowDropdownPopup(VisualElement anchor, DropdownToolOption dropdown) {
			var rect = anchor.worldBound;
			if (float.IsNaN(rect.x) || float.IsNaN(rect.y) || float.IsNaN(rect.width) || rect.width <= 0f || rect.height <= 0f) {
				Logger.LogError($"Cannot open dropdown '{dropdown.Label}': invalid anchor rect ({rect}).", tag: nameof(Window), context: this);
				return;
			}

			var menu = new TreeMenuWindow(
				new AdvancedDropdownState(),
				dropdown.Label,
				dropdown.Choices,
				dropdown.Value,
				value => {
					dropdown.Select(value);
					UpdateOptions();
				}
			);

			menu.Show(rect);
		}

		private void UpdateContent() {
			if (Content == null)
				return;
			Content.Clear();
			var active = GetActive();
			if (active == null) return;

			try {
				var content = active.GetContent();
				content.AddToClassList("flex-grow");
				Content.Add(content);
			} catch (Exception ex) {
				var panel = active.GetPanel();
				Editor.CoreAPI?.LoggerAPI?.LogException(
					new Exception($"GetContent() failed for panel '{panel?.GetLabel() ?? "?"}' (title: '{active.GetTitle()}')", ex),
					context: this
				);
			}
		}

		/// <summary>
		/// Makes the panel matching <paramref name="label"/> the active one in this window.
		/// </summary>
		private void ActivatePanel(string label) {
			var panels = PanelManager.GetPanels();
			foreach (var panel in panels) {
				if (panel.GetLabel() != label)
					continue;

				var active = GetActive();
				if (active != null && panel == active.GetPanel()) {
					Logger.LogDebug($"Panel '{label}' is already active. Focusing window.", tag: nameof(Window), context: this);
					Focus();
					return;
				}

				var instances = panel.GetInstances();
				if (!panel.AllowMultiple() && instances.Length > 0) {
					Logger.LogDebug($"Panel '{label}' does not allow multiple instances and one is already open. Focusing existing instance.", tag: nameof(Window), context: this);
					instances[0].GetWindow().Focus();
					return;
				}

				if (!SetActive(panel)) {
					Logger.LogError($"Failed to set active panel to '{label}'", tag: nameof(Window), context: this);
					return;
				}

				Repaint();
				return;
			}
		}

		public void CreateGUI() {
			rootVisualElement.Clear();
			_menu       = null;
			_breadcrumbs = null;
			_content    = null;
			_options    = null;
			rootVisualElement.AddToClassList("flex-grow");
			var content = Resources.Load<VisualTreeAsset>("Document").CloneTree();
			content.styleSheets.Add(Resources.Load<StyleSheet>("Style"));
			content.styleSheets.Add(Resources.Load<StyleSheet>("styles/index"));
			content.AddToClassList("flex-grow");
			rootVisualElement.Add(content);
			UpdateMenu();
			UpdateContent();
		}
	}
}