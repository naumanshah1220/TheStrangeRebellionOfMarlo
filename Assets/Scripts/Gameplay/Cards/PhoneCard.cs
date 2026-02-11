using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phone card data - example of extending the card system
/// </summary>
[CreateAssetMenu(fileName = "NewPhone", menuName = "Cards/Phone")]
public class PhoneCard : BaseCardData
{
    [Header("Phone Specific Data")]
    public string phoneNumber;
    public List<string> contactList = new List<string>();
    public List<string> messageHistory = new List<string>();
    public bool isLocked = true;
    public string unlockCode;

    public override CardMode GetCardMode() => CardMode.Phone;

    [Header("Phone Interactions")]
    public bool canMakeCalls = true;
    public bool canSendMessages = true;
    public bool hasInternet = false;
} 