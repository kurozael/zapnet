using System.Collections;
using System.Collections.Generic;
using Dissonance;
using Dissonance.Integrations.Zapnet;
using UnityEngine;
using UnityEngine.UI;

public class ServerWarning
    : MonoBehaviour
{
    private Text _text;
    private DissonanceComms _comms;
    private ZapnetCommsNetwork _net;

    void Start()
    {
        _text = GetComponent<Text>();
        _comms = GetComponentInParent<DissonanceComms>();
        _net = GetComponentInParent<ZapnetCommsNetwork>();
    }

    void Update()
    {
        if (_comms.IsNetworkInitialized && _net.Mode == Dissonance.Networking.NetworkMode.DedicatedServer)
            _text.enabled = true;
    }
}
