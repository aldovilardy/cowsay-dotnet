using CowSay.Core.Models;

namespace CowSay.Core.Interfaces;

/// <summary>
/// Defines methods for retrieving cow entities and their names from a data source.
/// </summary>
/// <remarks>
/// Provide access to cow data, typically for use in applications that manage or display information about cows. 
/// Methods may return null or empty results if no matching data is found.
/// Thread safety and data consistency depend on the specific implementation.
/// </remarks>
public interface ICowRepository
{
    /// <summary>
    /// Retrieves a cow by its name.
    /// </summary>
    /// <param name="name">The name of the cow to retrieve. Cannot be null or empty.</param>
    /// <returns>A <see cref="Cow"/> instance with the specified name, or null if no matching cow is found.</returns>
    Cow GetCowByName(string name);

    /// <summary>
    /// Retrieves a collection list of all available cows names.
    /// </summary>
    /// <returns>
    /// An enumerable collection of strings containing the names of all available cows names. 
    /// The collection will be empty if no cows are available.
    /// </returns>
    IEnumerable<string> ListAvailableCows();    
}
