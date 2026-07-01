using System.Collections.Generic;

[System.Serializable]
public class SeasonSchedule
{
    public int seasonNumber;
    public List<string> storyStarterIDs;
}

[System.Serializable]
public class ScheduleCollection
{
    public List<SeasonSchedule> schedules;
}