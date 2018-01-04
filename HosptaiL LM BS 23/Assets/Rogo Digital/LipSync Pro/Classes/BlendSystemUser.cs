using UnityEngine;

namespace RogoDigital.Lipsync {
	public class BlendSystemUser : MonoBehaviour {
		/// <summary>
		/// BlendSystem used
		/// </summary>
		public BlendSystem blendSystem;

		protected void OnDestroy () {
			blendSystem.Unregister(this);
		}

	}
}