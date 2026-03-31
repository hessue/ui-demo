using System;

namespace BlockAndDagger
{
    /*public interface IScenario
{
    public ScenarioType Type { }
    public string Description;
    public Wave Wave;
}*/

    [Serializable]
    public class BaseScenario //: IScenario
    {
        public ScenarioType scenarioType;

        public string description;

        //seconds
        public float interval;
        public int repeatTimes;

        ///Enables to send multiple waves from different directions 
        public Wave[] waves;
    }
}