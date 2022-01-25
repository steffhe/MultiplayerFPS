using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.UI;

public class PlayerInfo
{
    public ProfileData profile;
    public int actor;
    public short kills;
    public short deaths;

    public PlayerInfo (ProfileData p, int a, short k, short d)
    {
        this.profile = p;
        this.actor = a;
        this.kills = k;
        this.deaths = d;
    }
}


public enum GameState
{
    Waiting = 0,
    Starting = 1,
    Playing = 2,
    Ending = 3
}

public class Manager : MonoBehaviourPunCallbacks, IOnEventCallback
{

    public int mainmenu = 0;
    public int killcount = 20;
    public int matchlength = 180;

    public GameObject mapcam;

    public string playerPrefabString;
    public string playerPrefab;
    public Transform[] spawnPoints;

    public List<PlayerInfo> playerInfo = new List<PlayerInfo>();
    public int myind;

    private Text myKills;
    private Text myDeaths;
    private Text timer;

    public Transform leaderboard;
    private Transform endgame;

    private int currentMatchTime;
    private Coroutine timerCoroutine;

    public GameObject Hud;

    private GameState state = GameState.Waiting;



    public enum EventCodes : byte
    {
        NewPlayer,
        UpdatePlayers,
        ChangeStat,
        RefreshTimer
    }


    private void Start()
    {
        killcount = (int)PhotonNetwork.CurrentRoom.CustomProperties["killcount"];
        mapcam.SetActive(false);
        ValidateConnection();
        InitializeUI();
        InitializeTimer();
        NewPlayer_S(Launcher.myProfile);
        Spawn();
    }

    private void Update()
    {
        if (state == GameState.Ending) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Leaderboard(leaderboard);
        }else if (Input.GetKeyUp(KeyCode.Tab))
        {
           leaderboard.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }
        private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void Spawn()
    {
        StartCoroutine(Spawn(3f));

    }

    public void InitializeUI()
    {
        myKills = GameObject.Find("HUD/Stats/Kills").GetComponent<Text>();
        myDeaths = GameObject.Find("HUD/Stats/Deaths").GetComponent<Text>();
        timer = GameObject.Find("HUD/Timer/Text").GetComponent<Text>();
        leaderboard = GameObject.Find("HUD").transform.Find("Leaderboard").transform;
        endgame = GameObject.Find("Canvas").transform.Find("End Game").transform;
       

        RefreshMyStats();
    }

    private void InitializeTimer()
    {
        currentMatchTime = matchlength;
        RefreshTimerUI();
        if (PhotonNetwork.IsMasterClient)
        {
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    private void RefreshTimerUI()
    {
        string minutes = (currentMatchTime / 60).ToString("00");
        string seconds = (currentMatchTime % 60).ToString("00");
        timer.text = $"{minutes}:{seconds}";
    }

    private void RefreshMyStats()
    {
        if (playerInfo.Count > myind)
        {
            myKills.text = $"{playerInfo[myind].kills} Kills";
            myDeaths.text = $"{playerInfo[myind].deaths} Deaths";
        }
        else
        {
            myKills.text = "0 Kills";
            myDeaths.text = "0 Deaths";
        }

        if (leaderboard.gameObject.activeSelf) Leaderboard(leaderboard);
    }

    private void Leaderboard(Transform lb)
    {
        for (int i = 2; i < lb.childCount; i++)
        {
            Destroy(lb.GetChild(i).gameObject);
        }


        lb.Find("Header/Mode").GetComponent<Text>().text = "FREE FOR ALL";
        lb.Find("Header/Map").GetComponent<Text>().text = "Battlefield";

        GameObject playerCard = lb.GetChild(1).gameObject;
        playerCard.SetActive(false);

        List<PlayerInfo> sorted = SortPlayers(playerInfo);

        bool alternateColors = false;
        foreach(PlayerInfo a in sorted)
        {
            GameObject newcard = Instantiate(playerCard, lb) as GameObject;

            if (alternateColors) newcard.GetComponent<Image>().color = new Color32(0, 0,0, 100);

            newcard.transform.Find("Level").GetComponent<Text>().text = a.profile.level.ToString("00");
            newcard.transform.Find("Username").GetComponent<Text>().text = a.profile.username;
            newcard.transform.Find("Score Value").GetComponent<Text>().text = (a.kills * 100).ToString();
            newcard.transform.Find("Kills Value").GetComponent<Text>().text = a.kills.ToString();
            newcard.transform.Find("Deaths Value").GetComponent<Text>().text = a.deaths.ToString();

            newcard.SetActive(true);
        }

        lb.gameObject.SetActive(true);

    }

    private List<PlayerInfo> SortPlayers (List<PlayerInfo> info)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while (sorted.Count < info.Count)
        {
            short highest = -1;
            PlayerInfo selection = info[0];

            foreach (PlayerInfo a in info)
            {
                if (sorted.Contains(a)) continue;
                if (a.kills > highest)
                {
                    selection = a;
                    highest = a.kills;
                }
            }

            sorted.Add(selection);
        }

        return sorted;
    }

    private void ValidateConnection() {
        if (PhotonNetwork.IsConnected) return;
        SceneManager.LoadScene(mainmenu);
    }

    private void StateCheck()
    {
        if (state == GameState.Ending)
        {
            EndGame();
        }
    }

    public void NewPlayer_S(ProfileData p)
    {
        object[] package = new object[6];

        package[0] = p.username;
        package[1] = p.level;
        package[2] = p.xp;
        package[3] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[4] = (short)0;
        package[5] = (short)0;

        PhotonNetwork.RaiseEvent((byte)EventCodes.NewPlayer, package, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, new SendOptions { Reliability = true });

    }
    public void NewPlayer_R(object[] data)
    {

        PlayerInfo p = new PlayerInfo(new ProfileData((string)data[0], (int)data[1], (int)data[2], 150f, 50f), (int)data[3], (short)data[4], (short)data[5]);

        playerInfo.Add(p);

        UpdatePlayers_S((int)state, playerInfo);

    }
    public void UpdatePlayers_S(int state, List<PlayerInfo> info)
    {
        object[] package = new object[info.Count + 1];

        package[0] = state;
        for(int i = 0; i < info.Count; i++)
        {
            object[] piece = new object[6];

            piece[0] = info[i].profile.username;
            piece[1] = info[i].profile.level;
            piece[2] = info[i].profile.xp;
            piece[3] = info[i].actor;
            piece[4] = info[i].kills;
            piece[5] = info[i].deaths;

            package[i + 1] = piece;
        }


        PhotonNetwork.RaiseEvent((byte)EventCodes.UpdatePlayers, package, new RaiseEventOptions { Receivers = ReceiverGroup.All }, new SendOptions { Reliability = true });

    }

    public void UpdatePlayers_R(object[] data)
    {
        state = (GameState)data[0];
        playerInfo = new List<PlayerInfo>();


        for (int i = 1; i < data.Length; i++)
        {
            object[] extract = (object[])data[i];

            PlayerInfo p = new PlayerInfo(new ProfileData((string)extract[0], (int)extract[1], (int)extract[2], 150f, 150f), (int)extract[3], (short)extract[4], (short)extract[5]);

            playerInfo.Add(p);

            if (PhotonNetwork.LocalPlayer.ActorNumber == p.actor) myind = i - 1;
        }

        StateCheck();

    }

    public void ChangeStat_S(int actor, byte stat, byte amt)
    {
        object[] package = new object[] {actor, stat, amt };

        PhotonNetwork.RaiseEvent((byte)EventCodes.ChangeStat, package, new RaiseEventOptions { Receivers = ReceiverGroup.All }, new SendOptions { Reliability = true });

    }

    public void ChangeStat_R(object[] data)
    {
        int actor = (int)data[0];
        byte stat = (byte)data[1];
        byte amt = (byte)data[2];


        for (int i = 0; i < data.Length; i++)
        {
            if (playerInfo[i].actor == actor)
            {
                switch (stat)
                {
                    case 0:
                        playerInfo[i].kills += amt;
                        Debug.Log($"Player {playerInfo[i].profile.username } : kills = {playerInfo[i].kills }");
                        break;

                    case 1:
                        playerInfo[i].deaths += amt;
                        Debug.Log($"Player {playerInfo[i].profile.username } : deaths = {playerInfo[i].deaths }");
                        break;
                }

                if (i == myind) RefreshMyStats();

                if (leaderboard.gameObject.activeSelf) Leaderboard(leaderboard);

                break;
            }
        }

        ScoreCheck();
    }

    public void RefreshTimer_S()
    {
        object[] package = new object[] {currentMatchTime};


        PhotonNetwork.RaiseEvent((byte)EventCodes.RefreshTimer, package, new RaiseEventOptions { Receivers = ReceiverGroup.All }, new SendOptions { Reliability = true });

    }

    public void RefreshTimer_R(object[] data)
    {
        currentMatchTime = (int)data[0];
        RefreshTimerUI();
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;

        EventCodes e = (EventCodes)photonEvent.Code;
        object[] o = (object[])photonEvent.CustomData;

        switch (e)
        {
            case EventCodes.NewPlayer:
                NewPlayer_R(o);
                    break;
            case EventCodes.UpdatePlayers:
                UpdatePlayers_R(o);
                break;
            case EventCodes.ChangeStat:
                ChangeStat_R(o);
                break;
            case EventCodes.RefreshTimer:
                RefreshTimer_R(o);
                break;
                
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(mainmenu);
    }

    private void ScoreCheck()
    {
        bool detectwin = false;

        foreach (PlayerInfo a in playerInfo)
        {
            if (a.kills >= killcount)
            {
                detectwin = true;
                    break;
            }
        }

        if (detectwin)
        {
            if (PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                UpdatePlayers_S((int)GameState.Ending, playerInfo);
            }
        }
    }

    private void EndGame()
    {
        state = GameState.Ending;

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        currentMatchTime = 0;
        RefreshTimerUI();
         
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }

        mapcam.SetActive(true);

        endgame.gameObject.SetActive(true);
        Leaderboard(endgame.Find("Leaderboard"));

        StartCoroutine(End(6f));
    }

    private IEnumerator End(float wait)
    {
        yield return new WaitForSeconds(wait);

        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
    }

    private IEnumerator Spawn(float wait)
    {
        mapcam.SetActive(true);
        Hud.SetActive(false);
        yield return new WaitForSeconds(wait);
        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
        mapcam.SetActive(false);
        Hud.SetActive(true);
        PhotonNetwork.Instantiate(playerPrefab, spawn.position, spawn.rotation);
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(1);

        currentMatchTime -= 1;
        if (currentMatchTime < 0)
        {
            timerCoroutine = null;
            UpdatePlayers_S((int)GameState.Ending, playerInfo);
        }
        else
        {
            RefreshTimer_S();
            timerCoroutine = StartCoroutine(Timer());
        }
    }
}
