using System;
using UnityEngine;

namespace GridTutorial
{
    public sealed class GridBuilding : MonoBehaviour
    {
        public GridBoard Board { get; private set; }
        public Vector2Int Anchor { get; private set; }
        public Vector2Int Size { get; private set; }
        [field: SerializeField] public float Health { get; private set; }
        public GridBuildDefinition Definition { get; private set; }
        public event Action<float> HealthChanged;
        public event Action<float> Damaged;
        bool registered;
        internal bool Initialize(GridBoard board, GridBuildDefinition definition, Vector2Int anchor, bool rotated)
        {
            Board = board; Definition = definition; Anchor = anchor; Size = definition.Footprint(rotated);
            Health = definition.maxHealth;
            registered = board.RegisterBuilding(this, anchor, Size);
            return registered;
        }
        public void TakeDamage(float damage)
        {
            if (!registered || Health <= 0 || damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage)) return;
            float applied = Mathf.Min(Health, damage);
            Health -= applied;
            Damaged?.Invoke(applied);
            HealthChanged?.Invoke(Health);
            if (Health <= 0) { gameObject.SetActive(false); Destroy(gameObject); }
        }
        void OnEnable()
        {
            if (!Board || registered || Health <= 0) return;
            registered = Board.RegisterBuilding(this, Anchor, Size);
            if (!registered) { Debug.LogWarning("Building cannot reoccupy its footprint.", this); gameObject.SetActive(false); }
        }
        void OnDisable()
        {
            if (registered && Board) Board.ReleaseBuilding(this, Anchor, Size);
            registered = false;
        }
    }
}
