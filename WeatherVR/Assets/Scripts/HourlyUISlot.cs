using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// Attach this directly to your Hourly Slot Prefab
public class HourlyUISlot : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI idText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI tempText;
    public TextMeshProUGUI rainChanceText;
    public RawImage iconImage;
}