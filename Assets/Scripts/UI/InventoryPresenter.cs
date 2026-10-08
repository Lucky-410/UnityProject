using Relicfall.Items;
using UnityEngine;

namespace Relicfall.UI
{
    public sealed class InventoryPresenter : MonoBehaviour
    {
        private InventoryUI source;
        private InventoryUguiView view;
        private bool dirty, wasHud, wasBag;
        public int DataRefreshCount { get; private set; }
        public void Bind(InventoryUI controller, InventoryUguiView target)
        {
            if (source != null) source.ViewChanged -= Changed;
            source = controller; view = target; dirty = true;
            wasHud = wasBag = false;
            if (source != null) source.ViewChanged += Changed;
            else view?.ClearSceneData();
        }
        private void Changed() => dirty = true;
        private void OnDestroy() { if (source != null) source.ViewChanged -= Changed; }
        public void Tick()
        {
            if (source == null || view == null) return;
            bool hud = view.Hud.gameObject.activeInHierarchy, bag = view.Bag.gameObject.activeInHierarchy;
            if (!hud && !bag) { wasHud = wasBag = false; return; }
            if (dirty || wasHud != hud || wasBag != bag)
            {
                view.Refresh(source.ReadViewData(), source.SelectedSlot, source.Filter);
                dirty = false; DataRefreshCount++;
            }
            else view.RefreshDash(source.DashReady);
            wasHud = hud; wasBag = bag;
        }
    }
}
