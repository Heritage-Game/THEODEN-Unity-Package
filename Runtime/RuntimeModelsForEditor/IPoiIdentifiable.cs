using UnityEngine;
/// <summary>
/// This interface has the purpose to make the poiId field of the template easily identifiable for validation
/// and export logic of <see cref="POITemplate"/>
/// BUT
/// This Interface can be used in the code to retrieve any other kind of id that belongs to a class that implements it
/// as follows:
/// private string GetPoiId(IPoiIdentifiable identifiable)
///{
///    return identifiable.poiId;
///}
/// </summary>
public interface IPoiIdentifiable
{
    string poiId { get; }
}
