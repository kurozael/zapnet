using UnityEngine;
using UnityEngine.SceneManagement;
using zapnet;

public class DissonanceZapnetInitializer : MonoBehaviour
{
    [Header("Game Settings")]

    [Header("Connection Settings")]
    public int serverPort = 1227;
    public string serverHost = "127.0.0.1";
    public int serverVersion;

    [Header("Debug Settings")]
    public NetSimulation serverSimulation;
    public NetSimulation clientSimulation;

    private bool _started;

    public bool IsHeadless()
    {
        return (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null);
    }

    private void Awake()
    {
        Zapnet.Initialize();

        Zapnet.Network.RegisterPacket<LoginCredentials>();
        Zapnet.Network.RegisterPacket<PlayerInputEvent>();
        Zapnet.Network.RegisterPacket<WeaponFireEvent>();
        Zapnet.Network.RegisterPacket<DissonanceReceiveEvent>();
        Zapnet.Network.RegisterPacket<DissonanceTransmitEvent>();

        DontDestroyOnLoad(gameObject);
    }

    private void OnGUI()
    {
        if (_started)
            return;

        GUILayout.Label("Server Port");
        int.TryParse(GUILayout.TextField(serverPort.ToString(), 5), out serverPort);

        GUILayout.Label("Server Address");
        serverHost = GUILayout.TextField(serverHost);

        if (GUILayout.Button("Start Server"))
        {
            StartServer();
        }

        if (GUILayout.Button("Start Client"))
        {
            StartClient();
        }
    }

    private void Start()
    {
        if (IsHeadless())
            StartServer();
    }

    private void StartedNetwork()
    {
        Zapnet.Prefab.LoadAll("Network");

        Destroy(FindObjectOfType<Camera>().gameObject);
        SceneManager.LoadScene("DissonanceZapnetGameScene", LoadSceneMode.Additive);

        Invoke("Test", 20f);

        _started = true;
    }

    private void Test()
    {
        if (Zapnet.Network.IsClient)
        {
            Zapnet.Entity.PreventSpawning = false;
        }
    }

    private void StartClient()
    {
        Zapnet.Entity.PreventSpawning = true;
        Zapnet.Network.Connect(serverHost, serverPort, new ClientHandler(serverVersion), clientSimulation);

        StartedNetwork();
    }

    private void StartServer()
    {
        Zapnet.Network.Host(serverPort, new DissonanceZapnetServerHandler(serverVersion), serverSimulation);

        FindObjectOfType<Canvas>().gameObject.SetActive(true);

        StartedNetwork();
    }
}
