# ⚡ zapnet

## ⚡ building

Ideally you would use Visual Studio 2017/2019 to build Zapnet.

1. You will need to add a reference to UnityEngine.dll which can be found in your local installation of Unity.
2. You will then be able to build using either the Release or Debug configurations.

Output files can be found in the `Build/` directory.

## ⚡ about zapnet

Zapnet is a multiplayer games framework for Unity developed specifically for programmers.

Many other assets try to make their frameworks more accessible to less experienced developers by integrating visual editing into the Unity Editor by sacrificing performance and speed.

Zapnet doesn't do this, and trusts that the programmer knows what they're doing.

We offer source code access on request via Gitlab, just message us on Discord after or before you purchase for more information.

## ⚡ useful links

Join us on Discord to join our active community and get support from us whenever you need it! Please PM us on Discord when you purchase with your invoice number, so we can give you access to exclusive customer channels.

* Discord: https://discord.gg/3ZvZ6PmB
* Documentation: https://zapnet.kurozael.com
* Source Code: https://github.com/kurozael/zapnet

## ⚡ documentation

Our documentation is ever expanding as we learn about the requirements of our customers.

* Fully documented C# API so that intellisense helps you understand what a class or method does
* Fully generated scripting reference including with the asset
* Online documentation at https://zapnet.dead.gg with a Getting Started guide and pages on all core Zapnet features
* Dedicated support from developers in our Discord server and a friendly userbase willing to help and share tips & tricks
* Example game is included that demonstrates movement and projectile synchronization

## ⚡ features

### ⚡ network prefabs
Instantiate any prefab with the NetworkPrefab component easily by transmitting its unique 2 byte identifier and instantiating it with one simple method call on the other side.

### ⚡ delivery modes
Makes use of all delivery modes that Lidgren supports such as Reliable, Unreliable Sequenced, Reliable Unordered, Reliable Sequenced and Reliable Ordered.

### ⚡ entities
Network entities are the main concept behind Zapnet, all objects you want to be replicated across the network will inherit the entity class. By default, the position and rotation of entities are automatically synchronized and interpolated smoothly.

### ⚡ states
Every entity has its own state class that inherits a basic state class. The state will contain variables relating to the entity that need to be sent very frequently with entity state updates. It's very simple to use.

### ⚡ events
With the event system you can create an event data type that can be serialized and sent over the network, deserialized, and then invoked on all subscribers for that event. Events can be transmitted globally or for a specific entity.

### ⚡ synchronized variables
Entities can have synchronized variables that are separate to state variables and are only synchronized when they have changed.

### ⚡ remote calls
While it is recommended to use Events because they are faster and more performant, you can use remote calls which are Zapnet's version of the RPC. They do not use reflection like some other networking alternatives.

### ⚡ network hitboxes
A build in network hitbox rewinding system. Simply attach the NetworkHitbox component to an entity (or any prefab) with a trigger collider. When a raycast is performed using Zapnet's raycast methods, hitboxes will be automatically rewound to the position they were at on a particular server tick before the raycast is made.

### ⚡ controllables
The base controllable entity can be inherited by your player class to handle the transmitting of input data, client-side prediction, and server reconciliation with very minimal effort on your part except from defining which inputs are being pressed and how to handle the application of those inputs with regards to your character controller.

### ⚡ entity subsystems
Entity subystems are additional components that can be added to entities. Each subsystem can contain its own Synchronized Variables, State, and can make Remote Calls. Subsystems have an Entity accessor to get the entity they belong to. Infact, an Entity is just a Subsystem, so everything a subsystem can do an entity can do too.
