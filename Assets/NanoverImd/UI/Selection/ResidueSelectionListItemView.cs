using System;
using NanoverImd.Selection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NanoverImd.UI
{
    /// <summary>
    /// Displays one residue selection in the selection-panel list.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResidueSelectionListItemView : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private Image background;

        [SerializeField]
        private TMP_Text nameLabel;

        [SerializeField]
        private TMP_Text summaryLabel;

        [SerializeField]
        private Color activeBackground =
            new Color(0.62f, 0.12f, 0.42f, 0.95f);

        [SerializeField]
        private Color inactiveBackground =
            new Color(0.13f, 0.16f, 0.22f, 0.92f);

        [SerializeField]
        private Color activeText = Color.white;

        [SerializeField]
        private Color inactiveText =
            new Color(0.84f, 0.87f, 0.92f, 1f);

        private ResidueSelection selection;
        private Action<ResidueSelection> selected;

        /// <summary>
        /// The selection represented by this row.
        /// </summary>
        public ResidueSelection Selection => selection;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClicked);
        }

        /// <summary>
        /// Connects this row to one selection and its click callback.
        /// </summary>
        public void Bind(ResidueSelection nextSelection,
                         Action<ResidueSelection> onSelected)
        {
            selection = nextSelection;
            selected = onSelected;
        }

        /// <summary>
        /// Refreshes the row contents and active-selection appearance.
        /// </summary>
        public void Refresh(bool isActive)
        {
            if (selection == null)
                return;

            if (nameLabel != null)
                nameLabel.text = selection.Name;

            if (summaryLabel != null)
                summaryLabel.text = $"{selection.Count} residues";

            if (background != null)
                background.color = isActive
                                       ? activeBackground
                                       : inactiveBackground;

            var textColor = isActive ? activeText : inactiveText;
            if (nameLabel != null)
                nameLabel.color = textColor;
            if (summaryLabel != null)
                summaryLabel.color = textColor;
        }

        private void OnClicked()
        {
            if (selection != null)
                selected?.Invoke(selection);
        }
    }
}
