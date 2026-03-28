namespace CowSay.Core.Models;

/// <summary>
/// IT defines the various modes that a cow can be in, each mode corresponds to a specific visual representation of the cow's face.
/// </summary>
public enum CowMode
{
    /// <summary>
    /// The default cow mode, representing the standard appearance of the cow.
    /// </summary>
    Default,
    /// <summary>
    /// "Borg mode," displaying an ASCII cow with == for eyes, referencing the Borg from Star Trek.
    /// </summary>
    Borg,    // ==
    /// <summary>
    /// "Dead mode," showing an ASCII cow with XX for eyes, indicating a lifeless or comical state.
    /// </summary>
    Dead,    // xx
    /// <summary>
    /// "Greedy mode," displaying an ASCII cow with $$ for eyes, representing a greedy or money-focused expression.
    /// </summary>
    Greedy,  // $$
    /// <summary>
    /// "Paranoid mode" displaying an ASCII cow with @@ for eyes, representing a state of paranoia.
    /// </summary>
    Paranoid,// @@
    /// <summary>
    /// "Stoned mode," showing an ASCII cow with ** for eyes, indicating a relaxed or altered state.
    /// </summary>
    Stoned,  // **
    /// <summary>
    /// "Tired mode," displaying an ASCII cow with -- for eyes, representing fatigue or exhaustion.
    /// </summary>
    Tired,   // --
    /// <summary>
    /// "Wired mode," showing an ASCII cow with LL for eyes, indicating high energy or alertness.
    /// </summary>
    Wired,   // LL
    /// <summary>
    /// "Youthful mode," displaying an ASCII cow with .. for eyes, representing a youthful or innocent expression.
    /// </summary>
    Youthful // ..
}
