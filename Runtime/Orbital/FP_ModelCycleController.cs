namespace FuzzPhyte.Placement.OrbitalCamera
{
    using System;
    using UnityEngine;
    [DisallowMultipleComponent]
    public sealed class FP_ModelCycleController : MonoBehaviour
    {
        [Header("Model Set (bindings)")]
        [SerializeField] private FP_ModelDisplayBinding[] _models;

        [Header("Placement")]
        [Tooltip("Where the active model should be positioned (center of bounds).")]
        [SerializeField] private Transform _displayPivot;

        [Tooltip("If true, moves active model to pivot position.")]
        [SerializeField] private bool _snapToPivot = true;

        [Tooltip("If true, resets local rotation of active model at pivot.")]
        [SerializeField] private bool _resetRotationOnShow = true;

        [Header("State")]
        [SerializeField] private int _activeIndex;
        [SerializeField] private FP_ToolbarAction _activeVisualAction;

        public int ActiveIndex => _activeIndex;
        public FP_ToolbarAction ActiveVisualAction => _activeVisualAction;
        public int Count => _models?.Length ?? 0;
        public FP_ModelDisplayBinding ActiveModel => (Count > 0 && _activeIndex >= 0 && _activeIndex < Count) ? _models[_activeIndex] : null;

        public event Action<int, FP_ModelDisplayBinding> OnActiveModelChanged;
        public event Action<int, FP_ModelDisplayBinding, FP_ToolbarAction> OnActiveVisualActionChanged;

        private void Awake()
        {
            ApplyActiveModel(force: true);
        }

        public void SetModels(FP_ModelDisplayBinding[] models, int startIndex = 0)
        {
            _models = models;
            _activeIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, Count - 1));
            ApplyActiveModel(force: true);
        }

        public void Next()
        {
            if (Count == 0) return;
            _activeIndex = (_activeIndex + 1) % Count;
            ApplyActiveModel(force: false);
        }

        public void Prev()
        {
            if (Count == 0) return;
            _activeIndex = (_activeIndex - 1 + Count) % Count;
            ApplyActiveModel(force: false);
        }
        public void SetVisualInformation(FP_ToolbarAction action)
        {
            _activeVisualAction = action;
            OnActiveVisualActionChanged?.Invoke(_activeIndex, ActiveModel, _activeVisualAction);
        }
        public void SetIndex(int index)
        {
            if (Count == 0) return;
            int clamped = Mathf.Clamp(index, 0, Count - 1);
            if (clamped == _activeIndex) return;

            _activeIndex = clamped;
            ApplyActiveModel(force: false);
        }

        public void HideAll()
        {
            if (_models == null) return;
            for (int i = 0; i < _models.Length; i++)
            {
                if (_models[i] != null) _models[i].gameObject.SetActive(false);
            }
        }

        private void ApplyActiveModel(bool force)
        {
            if (_models == null || _models.Length == 0)
            {
                OnActiveModelChanged?.Invoke(-1, null);
                return;
            }

            // Hide all except active
            for (int i = 0; i < _models.Length; i++)
            {
                var go = _models[i];
                if (go == null) continue;

                bool shouldShow = (i == _activeIndex);
                if (force || go.gameObject.activeSelf != shouldShow)
                    go.gameObject.SetActive(shouldShow);
            }

            var active = _models[_activeIndex];
            if (active != null && _displayPivot != null)
            {
                if (_snapToPivot)
                    active.transform.position = _displayPivot.position;

                if (_resetRotationOnShow)
                    active.transform.rotation = Quaternion.identity;
            }

            OnActiveModelChanged?.Invoke(_activeIndex, active);
        }
    }
}
