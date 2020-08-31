using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cylib
{
    public interface IScene
    {
        void Update(float dt);

        /// <summary>
        /// Called once around Load time.
        /// Scene must use the camera returned by this method for the entirety of the scene.
        /// </summary>
        ICamera GetCamera();

        /// <summary>
        /// Should return the list of assets this scene is expecting to load.
        /// If an asset isn't added to this list, but is referenced during load,
        /// the asset will be loaded there.
        /// 
        /// Generally better to include everything possible in this list, because the AssetManager will
        /// dispose of potentially unused assets before the load begins.
        /// </summary>
        HashSet<int> GetAssetList();

        /// <summary>
        /// Should return the list of assets this scene wants during load time.
        /// </summary>
        HashSet<int> GetPreloadAssetList();

        /// <summary>
        /// Minimum time in seconds to spend on loading. (Useful for splash screens)
        /// </summary>
        /// <returns></returns>
        float LoadTime();

        /// <summary>
        /// Initialize preload assets here.
        /// </summary>
        void Preload(EventManager em);

        /// <summary>
        /// Update called during load
        /// </summary>
        void LoadUpdate(float dt);

        /// <summary>
        /// Called when load ends. Good place to clean up variables used during load.
        /// Called in main thread, not loading thread.
        /// </summary>
        void LoadEnd();

        /// <summary>
        /// Initialize scene assets here. All calls to use an asset must be performed here, or returned from getAssetList, before they can be used anywhere else.
        /// Generally the good place to hook UI events, too.
        /// 
        /// This is run in a separate thread from everything else -- do any intesive processing for preparing the scene here.
        /// </summary>
        void Load(EventManager em);

        /// <summary>
        /// Whether this scene should draw 3D at all, or if it just needs the 2D rendering step.
        /// Is checked every frame.
        /// </summary>
        /// <returns></returns>
        bool Draw3D();

        void Dispose();
    }
}
