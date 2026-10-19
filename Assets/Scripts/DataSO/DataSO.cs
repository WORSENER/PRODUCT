using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataClass;



[CreateAssetMenu(fileName ="new data",menuName ="Data",order =0)]
public class DataSO : ScriptableObject
{
    public List<YearEnergyConsumption> yearEnergyConsumptions = new List<YearEnergyConsumption>();
    
    public List<MonthEnergyConsumption> monthEnergyConsumptions = new List<MonthEnergyConsumption>();
    
    public List<DayEnergyConsumption> dayEnergyConsumptions = new List<DayEnergyConsumption>();
    
    public List<YearDepartmentEnergyConsumption> yearDepartmentEnergyConsumptions = new List<YearDepartmentEnergyConsumption>();
   
    public List<MonthDepartmentEnergyConsumption> monthDepartmentEnergyConsumptions = new List<MonthDepartmentEnergyConsumption>();
    
    public List<DayDepartmentEnergyConsumption> dayDepartmentEnergyConsumptions = new List<DayDepartmentEnergyConsumption>();
    
    public List<WelcomeMessage> welcomeMessages = new List<WelcomeMessage>();
    public FloorInfo floorInfo = new FloorInfo();

    public void EnsureInitialized()
    {
        yearEnergyConsumptions ??= new List<YearEnergyConsumption>();
        monthEnergyConsumptions ??= new List<MonthEnergyConsumption>();
        dayEnergyConsumptions ??= new List<DayEnergyConsumption>();
        yearDepartmentEnergyConsumptions ??= new List<YearDepartmentEnergyConsumption>();
        monthDepartmentEnergyConsumptions ??= new List<MonthDepartmentEnergyConsumption>();
        dayDepartmentEnergyConsumptions ??= new List<DayDepartmentEnergyConsumption>();
        welcomeMessages ??= new List<WelcomeMessage>();
        floorInfo ??= new FloorInfo();
    }
}

