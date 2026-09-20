using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Nox.Editor.Panel.Runtime {
	/// <summary>
	/// Hierarchical, searchable popup menu built exactly like Unity's "Add Component" menu: a class
	/// deriving from <see cref="AdvancedDropdown"/>, which displays one level at a time (with a back
	/// button and a header) and exposes a search field that flattens the whole tree.
	/// Same pattern as <c>TypeSearchWindow</c> in <c>Packages/nox.jint</c>.
	///
	/// Choices are '/'-separated paths, typically <c>IPanel.GetLabel()</c> ("Editor/Logger"): they become
	/// submenus. A choice without any '/' behaves as a flat list. The current value is prefixed with a
	/// check mark.
	/// </summary>
	internal class TreeMenuWindow : AdvancedDropdown {
		private const string CheckPrefix   = "\u2713 ";
		private const string PathSeparator = "/";

		/// <summary>
		/// An item carrying the value it selects. <c>name</c> is deliberately the full path while the
		/// displayed text stays the segment: <see cref="AdvancedDropdown"/> matches the search on
		/// <c>name</c> but draws the text given to the constructor, which gives path-based search with
		/// segment-based display (the constructor does not refresh the content text).
		/// </summary>
		private sealed class ValueItem : AdvancedDropdownItem {
			public readonly string Value;

			public ValueItem(string display, string path) : base(display) {
				Value = path;
				name  = path;
			}
		}

		private readonly string         _title;
		private readonly List<string>   _paths;
		private readonly string         _selected;
		private readonly Action<string> _onSelect;

		public TreeMenuWindow(
			AdvancedDropdownState state,
			string title,
			IEnumerable<string> paths,
			string selected,
			Action<string> onSelect) : base(state) {
			_title    = string.IsNullOrEmpty(title) ? "Menu" : title;
			_paths    = new List<string>(paths ?? Array.Empty<string>());
			_selected = selected;
			_onSelect = onSelect;

			minimumSize = new Vector2(260, 320);
		}

		protected override AdvancedDropdownItem BuildRoot() {
			var root   = new AdvancedDropdownItem(_title);
			var nodes  = new Dictionary<string, AdvancedDropdownItem>(StringComparer.Ordinal) { { string.Empty, root } };
			var leaves = CollectLeaves();

			// Paths used as a container for another leaf must stay navigable, even when they are a leaf
			// themselves (a panel that is also the parent of another one).
			var containers = new HashSet<string>(StringComparer.Ordinal);
			foreach (var leaf in leaves)
				foreach (var prefix in Prefixes(leaf))
					containers.Add(prefix);

			foreach (var leaf in leaves) {
				var parent = root;
				var path   = string.Empty;

				foreach (var segment in leaf.Split('/')) {
					var name = segment.Trim();
					if (name.Length == 0)
						continue;

					path = path.Length == 0 ? name : $"{path}{PathSeparator}{name}";

					if (nodes.TryGetValue(path, out var existing)) {
						parent = existing;
						continue;
					}

					var node = path == leaf && !containers.Contains(path)
						? new ValueItem(Display(name, path), path)
						: new AdvancedDropdownItem(name);

					nodes[path] = node;
					parent.AddChild(node);
					parent = node;
				}
			}

			// A panel whose label is a prefix of another one also needs its own entry: inside its level.
			foreach (var leaf in leaves) {
				if (!containers.Contains(leaf) || !nodes.TryGetValue(leaf, out var container))
					continue;

				var name = LastSegment(leaf);
				container.AddChild(new ValueItem(Display(name, leaf), leaf));
			}

			return root;
		}

		protected override void ItemSelected(AdvancedDropdownItem item) {
			// `item` is null when the window is closed without picking anything, and containers are not
			// selectable, so only ValueItems carry a selection.
			if (item is ValueItem valueItem && !string.IsNullOrEmpty(valueItem.Value))
				_onSelect?.Invoke(valueItem.Value);
		}

		private List<string> CollectLeaves() {
			var leaves = new List<string>();

			foreach (var path in _paths) {
				if (string.IsNullOrEmpty(path) || leaves.Contains(path))
					continue;

				leaves.Add(path);
			}

			return leaves;
		}

		private string Display(string name, string path)
			=> string.Equals(path, _selected, StringComparison.Ordinal) ? $"{CheckPrefix}{name}" : name;

		private static string LastSegment(string path) {
			var index = path.LastIndexOf('/');
			return index < 0 ? path : path.Substring(index + 1);
		}

		private static IEnumerable<string> Prefixes(string path) {
			var index = path.IndexOf('/');
			while (index >= 0) {
				yield return path.Substring(0, index);
				index = path.IndexOf('/', index + 1);
			}
		}
	}
}
