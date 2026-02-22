using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class FamilyStatus
{
    public float hunger;           // 0-100, 0 = starving, 100 = well fed
    public float savings;          // Current money saved
    public float dailyExpenses;    // How much needed per day
    public bool isStarving;        // True if hunger < threshold for too long
}

[System.Serializable]
public class ResistanceProgress
{
    public float suspicionLevel;   // 0-100, how much the state suspects you
    public float trustLevel;       // 0-100, how much the resistance trusts you
    public List<string> helpedResistanceIds;  // IDs of cases where you helped resistance
    public List<string> helpedStateIds;       // IDs of cases where you helped state
}

public class PlayerProgressManager : SingletonMonoBehaviour<PlayerProgressManager>
{

    public FamilyStatus familyStatus = new FamilyStatus();
    public ResistanceProgress resistanceProgress = new ResistanceProgress();

    [Header("Family Settings")]
    public float hungerDecreasePerDay = 20f;
    public float starvingThreshold = 30f;
    public float baseIncome = 35f;
    public float baseDailyExpense = 30f;

    public event Action<float> OnMoneyEarned;
    public event Action<float> OnHungerChanged;
    public event Action<bool> OnStarvingStateChanged;
    public event Action<float> OnSuspicionChanged;
    public event Action<float> OnResistanceTrustChanged;
    public event Action<float> OnSuspicionLevelChanged;

    protected override void OnSingletonAwake()
    {
        InitializeStatus();
        GameEvents.OnCaseSolved += HandleCaseSolved;
    }

    protected override void OnSingletonDestroy()
    {
        GameEvents.OnCaseSolved -= HandleCaseSolved;
    }

    private void HandleCaseSolved(Case solvedCase)
    {
        if (solvedCase == null) return;

        AddReward(solvedCase.reward);

        if (!string.IsNullOrEmpty(solvedCase.lawBroken))
        {
            AddReward(solvedCase.extraRewardForState);
            ReduceSuspicion(solvedCase.suspicionReduction);
        }
    }

    private void InitializeStatus()
    {
        familyStatus.hunger = 100f;
        familyStatus.savings = 100f;
        familyStatus.dailyExpenses = baseDailyExpense;
        familyStatus.isStarving = false;

        resistanceProgress.suspicionLevel = 0f;
        resistanceProgress.trustLevel = 0f;
        resistanceProgress.helpedResistanceIds = new List<string>();
        resistanceProgress.helpedStateIds = new List<string>();
    }

    public void ProcessDayEnd(float dayEarnings, bool helpedResistance = false)
    {
        // Process earnings
        familyStatus.savings += dayEarnings;
        OnMoneyEarned?.Invoke(dayEarnings);

        // Process daily expenses
        familyStatus.savings -= familyStatus.dailyExpenses;

        // Update hunger based on if we could afford food
        if (familyStatus.savings >= 0)
        {
            familyStatus.hunger = Mathf.Min(100f, familyStatus.hunger + 20f);
        }
        else
        {
            familyStatus.hunger = Mathf.Max(0f, familyStatus.hunger - hungerDecreasePerDay);
        }

        // Check starving state
        bool wasStarving = familyStatus.isStarving;
        familyStatus.isStarving = familyStatus.hunger < starvingThreshold;
        
        if (wasStarving != familyStatus.isStarving)
        {
            OnStarvingStateChanged?.Invoke(familyStatus.isStarving);
        }

        OnHungerChanged?.Invoke(familyStatus.hunger);
    }

    public void RecordResistanceChoice(string caseId, bool helpedResistance)
    {
        if (helpedResistance)
        {
            resistanceProgress.helpedResistanceIds.Add(caseId);
            resistanceProgress.trustLevel = Mathf.Min(100f, resistanceProgress.trustLevel + 10f);
            resistanceProgress.suspicionLevel = Mathf.Min(100f, resistanceProgress.suspicionLevel + 5f);
            
            OnResistanceTrustChanged?.Invoke(resistanceProgress.trustLevel);
            OnSuspicionChanged?.Invoke(resistanceProgress.suspicionLevel);
        }
        else
        {
            resistanceProgress.helpedStateIds.Add(caseId);
            resistanceProgress.trustLevel = Mathf.Max(0f, resistanceProgress.trustLevel - 5f);
            OnResistanceTrustChanged?.Invoke(resistanceProgress.trustLevel);
        }
    }

    public bool IsGameOver()
    {
        return familyStatus.isStarving || resistanceProgress.suspicionLevel >= 100f;
    }

    public void ReduceSuspicion(float amount)
    {
        // Reduce suspicion level by the specified amount
        float currentSuspicionLevel = resistanceProgress.suspicionLevel;
        resistanceProgress.suspicionLevel = Mathf.Max(0, currentSuspicionLevel - amount);
        
        // Notify any listeners that suspicion level has changed
        OnSuspicionLevelChanged?.Invoke(resistanceProgress.suspicionLevel);
        
        Debug.Log($"Suspicion reduced by {amount}. New level: {resistanceProgress.suspicionLevel}");
    }
    
    public void AddReward(float amount)
    {
        // Add money to savings
        familyStatus.savings += amount;

        // Notify listeners
        OnMoneyEarned?.Invoke(amount);

        Debug.Log($"Added reward: {amount}. New savings: {familyStatus.savings}");
    }

    /// <summary>
    /// Apply a bonus from the overseer's morning letter. Credits savings and fires OnMoneyEarned.
    /// </summary>
    public void ApplyBonus(float amount)
    {
        if (amount <= 0f) return;

        familyStatus.savings += amount;
        OnMoneyEarned?.Invoke(amount);

        Debug.Log($"[PlayerProgressManager] Bonus applied: +${amount:F0}. New savings: ${familyStatus.savings:F0}");
    }
} 