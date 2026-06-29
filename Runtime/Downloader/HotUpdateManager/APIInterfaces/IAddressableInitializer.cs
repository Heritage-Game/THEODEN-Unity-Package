using System.Threading.Tasks;
using UnityEngine;

public interface IAddressableInitializer
{
    /// <summary>
    /// Initializes the Addressable System. Some Unity version require the initializazion beforehand.
    /// Safe to use multiple times: it checks if the Addressable system is already initialized.
    /// </summary>
    /// <returns></returns>
    Task InitializeAsync();
  
}
