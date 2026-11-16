using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Plants
{
    /// <summary>
    /// Water Collector plant (contract 5.5): once grown it catches dew and produces one charge of water every
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
        ParticleSystem _drips, _sparkle;   // dew falling into the bowl while it fills; a sparkle over the water while a charge waits
        static readonly Color Dew = new Color(0.75f, 0.92f, 1f), DewBright = new Color(0.95f, 1f, 1f), Ready = new Color(0.49f, 0.95f, 0.9f);

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

        /// <summary>Drips while dew is gathering, sparkles while there is water to take. Built the first time either is needed.</summary>
        void ApplyEffects(bool filling, bool ready)
        {
            if (reservoir == null || (!filling && !ready && _drips == null)) return;
            if (_drips == null)
            {
                var bowl = reservoir.position;
                _drips = Motes.Ambient("Drips", transform, bowl + Vector3.up * 0.75f, new Vector3(0.45f, 0.1f, 0.45f), Dew, DewBright, 0.07f, 0.9f, -0.75f, 2.5f, 6);
                _sparkle = Motes.Ambient("ReadySparkle", transform, bowl + Vector3.up * 0.2f, new Vector3(0.6f, 0.15f, 0.6f), Ready, DewBright, 0.14f, 1.8f, 0.3f, 4f, 12);
            }
            var de = _drips.emission; if (de.enabled != filling) de.enabled = filling;
            var se = _sparkle.emission; if (se.enabled != ready) se.enabled = ready;
        }

        void ApplyFill()
        {
            bool grown = _plant == null || _plant.IsGrown;
            ApplyEffects(grown && !oneShot && Stored < capacity && WeatherState.DewFalls, grown && Stored > 0);
            if (reservoir == null) return;
            float f = _plant != null && !_plant.IsGrown ? 0f : Fill;
            reservoir.gameObject.SetActive(f > 0.01f);
            // the basin is narrow at the bottom, so the water widens as it rises (half width when empty): its edge stays hidden inside the basin wall
            float w = Mathf.Lerp(0.5f, 1f, Mathf.Sqrt(f));
            reservoir.localScale = new Vector3(_fullScale.x * w, Mathf.Max(0.001f, _fullScale.y * f), _fullScale.z * w);
        }
    }
}
