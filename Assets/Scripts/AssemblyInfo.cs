using System.Runtime.CompilerServices;

// The bootstrap helper and the editor scene builder need to configure manager
// components without exposing their wiring setters to gameplay code. We keep
// those setters `internal` and grant access only to our own tooling assemblies.
[assembly: InternalsVisibleTo("TowerDefense.Editor")]
[assembly: InternalsVisibleTo("TowerDefense.Tests.EditMode")]
[assembly: InternalsVisibleTo("TowerDefense.Tests.PlayMode")]
