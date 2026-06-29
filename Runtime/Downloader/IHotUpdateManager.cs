
using System.Collections;
using System.Collections.Generic;

namespace Downloader
{
    /// <summary>
    /// This interface provides the API to use the HotUpdateManager class, providing the methods to:
    /// - Check for updates in the remote catalogs
    /// - Update the assets into the local catalogs to the latest version
    /// - Retrieve assets from the remote catalogue on demand
    /// 
    /// </summary>
    public interface IHotUpdateManager
    {
        //forse questa classe dovrebbe ritornare il dowload size e chiedere all'utente se vuole eseguire il dowload
        //public void CheckForUpdates (IEnumerable<string> labelsOrKeys)
        //public void UpdateToCurrentVersion (IEnumerable<string> labelsOrKeys)
        /// <summary>
        /// This method dowloads assets on demand when called. The reference to assets is passed a collection of
        /// <code>string</code> that represent the lables given to the asset group or the reference to the asset.
        /// </summary>
        /// <param name="labelsOrKeys"></param>
        public void LoadAssets(IEnumerable<string> labelsOrKeys);

    }
}
