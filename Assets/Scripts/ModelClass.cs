using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using UnityEngine.UI;

[Serializable]
public class BetData
{
  public double currentBet { get; set; }
  public double currentLines { get; set; }
  public double spins { get; set; }
}

[Serializable]
public class MessageData
{
  public BetData data { get; set; }
  public string id { get; set; }
}

[Serializable]
public class GameData
{
  public List<List<int>> lines { get; set; }
  public List<double> Bets { get; set; }
  public List<List<int>> resultSymbols { get; set; }
  public List<int> linesToEmit { get; set; }
  public List<List<int>> symbolsToEmit { get; set; }
  public FreeSpin freeSpin { get; set; }
  public List<IsDouble> doubleLines { get; set; }
  public List<DaimondMultiplier> daimondMultipliers { get; set; }
}

public class DaimondMultiplier
{
  public List<int> range { get; set; }
  public int multiplier { get; set; }
}

public class IsDouble
{
  public double payout { get; set; }
}

[Serializable]
public class FreeSpin
{
  public bool isFreeSpin { get; set; }
  public int freeSpinCount { get; set; }
  public int freeSpinMultiplier { get; set; }
  public int diamondCount { get; set; }
  public double payout { get; set; }
}

[Serializable]
public class Message
{
  public GameData GameData { get; set; }
  public UIData UIData { get; set; }
  public PlayerData PlayerData { get; set; }
  public List<string> BonusData { get; set; }
}

[Serializable]
public class Root
{
  public string id { get; set; }
  public Message message { get; set; }
}

[Serializable]
public class UIData
{
  public Paylines paylines { get; set; }
}

[Serializable]
public class Paylines
{
  public List<Symbol> symbols { get; set; }
}

[Serializable]
public class Symbol
{
  public string Name { get; set; }
  [JsonProperty("multiplier")]
  public object MultiplierObject { get; set; }
  [JsonIgnore] public List<List<double>> Multiplier { get; private set; }
  [OnDeserialized]
  internal void OnDeserializedMethod(StreamingContext context)
  {
    if (MultiplierObject is JObject)
    {
      Multiplier = new List<List<double>>();
    }
    else
    {
      Multiplier = JsonConvert.DeserializeObject<List<List<double>>>(MultiplierObject.ToString());
    }
  }
  public object description { get; set; }
}
[Serializable]
public class PlayerData
{
  public double Balance { get; set; }
  public double currentWining { get; set; }
}
[Serializable]
public class AuthTokenData
{
  public string cookie;
  public string socketURL;
  public string nameSpace;
}

[Serializable]
public class SlotImage
{
  public List<Image> slotImages = new List<Image>(10);
}
