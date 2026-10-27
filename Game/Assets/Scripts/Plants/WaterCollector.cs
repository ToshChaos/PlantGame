using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Plants
{
    /// <summary>
    /// Water Collector plant (contract 5.5, T28): once grown it catches dew and produces one charge of water every
    /// `secondsPerCharge` (capacity `capacity`, default 1). F (Water) hands the stored charge to the watering can. The Reservoir child mesh
    /// rises with the fill (stored + progress). It needs no water to grow. Put on the plant root next to PlantInstance;
    /// the INTERACT_NN trigger box (layer Interactable) is what the player looks at.
    /// </summary>
    public class WaterCollector : MonoBehaviour, IInteractable
    {
        public float secondsPerCharge = 30f;
        public int capacity = 1;
        [Tooltip("A single-use dew patch: holds one charge from the start and never refills.")] public bool oneShot;
        public Transform reservoir;
        /// <summary>Rain levels: a charge takes 15 s of rain (30 s elsewhere) (the progress is kept between showers).</summary>
        float Seconds => WeatherState.RainLevel ? 15f : secondsPerCharge;

        PlantInstance _plant;
        float _progress;
        Vector3 _fullScale;

        public int Stored { get; private set; }
        /// <summary>0..1 toward the next charge (1 = full).</summary>
        public float Fill => Stored >= capacity ? 1f : (Stored + _progress / Mathf.Max(0.01f, Seconds)) / capacity;

        public InteractAction Action => InteractAction.Use;
        public string Prompt => "Collect water";
        public bool CanInteract(GameObject actor)
        {
            var can = actor != null ? actor.GetComponent<WateringCan>() : null;
            return Stored > 0 && can != null && !can.IsFull;
        }

        public void Interact(GameObject actor)
        {
            var can = actor != null ? actor.GetComponent<WateringCan>() : null;
            if (can == null || Stored <= 0) return;
            Stored -= can.Refill(Stored);
        }

        /// <summary>Single-use patch: one charge now, no dew afterwards.</summary>
        public void MakeOneShot(bool refills = false)
        {
            oneShot = !refills; capacity = Mathf.Max(1, capacity); Stored = 1; _progress = 0f;
            ApplyFill();
        }

        void Awake()
        {
            _plant = GetComponent<PlantInstance>();
            if (reservoir == null) foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "Reservoir") reservoir = t;
            if (reservoir != null) _fullScale = reservoir.localScale;
            ApplyFill();
        }

        void Update()
        {
            if (oneShot || (_plant != null && !_plant.IsGrown)) { ApplyFill(); return; }
            if (!WeatherState.DewFalls) { ApplyFill(); return; }   // rain levels: dew only gathers while it rains
            if (Stored < capacity)
            {
                _progress += Time.deltaTime;
                if (_progress >= Seconds) { _progress = 0f; Stored++; GameEvents.RaiseCollectorReady(); }
            }
            else _progress = 0f;
            ApplyFill();
        }

        void ApplyFill()
        {
            if (reservoir == null) return;
            float f = _plant != null && !_plant.IsGrown ? 0f : Fill;
            reservoir.gameObject.SetActive(f > 0.01f);
            reservoir.localScale = new Vector3(_fullScale.x, Mathf.Max(0.001f, _fullScale.y * f), _fullScale.z);
        }
    }
}
