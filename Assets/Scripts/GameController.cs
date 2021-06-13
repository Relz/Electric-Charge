using System;
using System.Collections;
using System.IO;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Firebase.Firestore;
using System.Collections.Generic;

public class GameController : MonoBehaviourPunCallbacks
{
    public Material[] PlayerMaterials;
    public GameObject ImageTarget;
    public GameObject Plate;
    public GameObject Minus;
    public GameObject Timer;
    public GameObject Hint;
    public GameObject GameOverWindow;
    public Text GameOverInfoMessage;
    public GameObject Stick;

    private PhotonView _photonView;
    private Text _timerValue;
    private Vector3 _plateCenter;
    private float _plateRadius;
    private GameObject _playerGameObject;
    private float _centeredSeconds = 0;
    private float _secondsPassed = 0;
    private int _integerSecondsPassed = 0;
    private bool _gameOver = false;

    private const int _goodTimeLimit = 60;
    private const int _normalTimeLimit = 90;

    private readonly Color _timerGoodColor = new Color32(107, 255, 133, 255);
    private readonly Color _timerNormalColor = new Color32(251, 210, 106, 255);
    private readonly Color _timerBadColor = new Color32(255, 43, 45, 255);


    public void Awake()
    {
        Application.targetFrameRate = 30;
        _photonView = GetComponent<PhotonView>();
        _timerValue = Timer.GetComponentInChildren<Text>();
        SetPlayerTargetFound(PhotonNetwork.LocalPlayer, false);
        _plateCenter = GetPlateCenter();
        _plateRadius = GetPlateRadius();
        _playerGameObject = InitializePlayer(Array.IndexOf(PhotonNetwork.PlayerList, PhotonNetwork.LocalPlayer));
        SetMinusActive(false);
    }

    public GameObject InitializePlayer(int playerIndex)
    {
        GameObject gameObject = PhotonNetwork.Instantiate(Path.Combine("In game", "ElectricCharge"), GeneratePosition(), Quaternion.identity, 0);

        int playerMaterialIndex = playerIndex % PlayerMaterials.Length;

        PhotonView photonView = gameObject.GetComponent<PhotonView>();
        photonView.RPC("SetPlayerMaterialIndex", RpcTarget.All, (object)playerMaterialIndex);
        photonView.RPC("SetPlateCenter", RpcTarget.All, (object)_plateCenter);

        return gameObject;
    }

    public void Update()
    {
        if (IsGameStarted() && !_gameOver)
        {
            Stick.SetActive(true);
            if (PhotonNetwork.IsMasterClient)
            {
                UpdateSecondsPassed();
                if (_secondsPassed > 3)
                {
                    CheckWinCondition();
                }
            }
        }
        if (_gameOver)
        {
            GameOverWindow.SetActive(true);
            Stick.SetActive(false);
        }
    }

    public void ExitGame()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        if (!_gameOver)
        {
            _gameOver = true;
            GameOverInfoMessage.text = "2-ой игрок вышел из игры =(";
        }
    }

    public void OnTargetFound()
    {
        SetPlayerTargetFound(PhotonNetwork.LocalPlayer, true);
        SetMinusActive(true);
        Timer.SetActive(true);
        Hint.SetActive(false);
    }

    public void OnTargetLost()
    {
        SetPlayerTargetFound(PhotonNetwork.LocalPlayer, false);
        SetMinusActive(false);
        Timer.SetActive(false);
        Hint.SetActive(true);
    }

    [PunRPC]
    public void SetSecondsPassed(int secondsPassed)
    {
        _integerSecondsPassed = secondsPassed;
        _timerValue.text = FormatSecondsPassed(secondsPassed);
        UpdateTimerColor(secondsPassed);
    }

    [PunRPC]
    public void Win()
    {
        _gameOver = true;
        GameOverInfoMessage.text = $"{GetCongratulationMessage()}\n\nВы прошли игру за {FormatSecondsPassed(_integerSecondsPassed)}";
    }

    public void OnMovement(InputAction.CallbackContext value)
    {
        _playerGameObject.GetComponent<Player>().OnMovement(value);
    }

    private void SetMinusActive(bool value)
    {
        Minus.GetComponent<Renderer>().enabled = value;
        Minus.GetComponent<Rigidbody>().isKinematic = !value;
        Minus.transform.position = new Vector3(Minus.transform.position.x, _plateCenter.y + 0.01f, Minus.transform.position.z);
    }

    private void UpdateTimerColor(int secondsPassed)
    {
        _timerValue.color = secondsPassed < _goodTimeLimit
            ? _timerGoodColor
            : secondsPassed < _normalTimeLimit
                ? _timerNormalColor
                : _timerBadColor;
    }

    private void SetPlayerTargetFound(Photon.Realtime.Player player, bool value)
    {
        ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
        hashtable.Add(PlayerCustomProperty.TargetFound, value);
        player.SetCustomProperties(hashtable);
    }

    private Vector3 GetPlateCenter()
    {
        return Plate.GetComponent<MeshRenderer>().bounds.center + new Vector3(0, Plate.GetComponent<MeshRenderer>().bounds.size.y * 0.9f, 0);
    }

    private float GetPlateRadius()
    {
        return Plate.GetComponent<MeshCollider>().bounds.size.x * 0.5f;
    }

    private Vector3 GeneratePosition()
    {
        Vector2 randomCirclePoint = GetRandomPointBetweenTwoCircles(_plateRadius * 0.55f, _plateRadius * 0.9f);
        return new Vector3(
            randomCirclePoint.x,
            Plate.transform.position.y + 0.01f,
            randomCirclePoint.y
        );
    }

    private Vector2 GetRandomPointBetweenTwoCircles(float minRadius, float maxRadius)
    {
        Vector2 randomUnitPoint = UnityEngine.Random.insideUnitCircle.normalized;
        return GetRandomBetweenTwoVector2(randomUnitPoint * minRadius, randomUnitPoint * maxRadius);
    }

    private Vector2 GetRandomBetweenTwoVector2(Vector2 min, Vector2 max)
    {
        return min + UnityEngine.Random.Range(0f, 1f) * (max - min);
    }

    private bool IsGameStarted()
    {
        return PhotonNetwork.PlayerList.All(GetPlayerTargetFound);
    }

    private bool GetPlayerTargetFound(Photon.Realtime.Player player)
    {
        return player.CustomProperties.ContainsKey(PlayerCustomProperty.TargetFound)
            && (bool)player.CustomProperties[PlayerCustomProperty.TargetFound];
    }

    private void CheckWinCondition()
    {
        if (CheckIsInCenter())
        {
            if (_centeredSeconds == 0)
            {
                StartCoroutine(WaitUntilCentered());
            }
            _centeredSeconds += Time.deltaTime;
        }
        else
        {
            _centeredSeconds = 0;
        }
    }

    bool CheckIsInCenter()
    {
        return Vector3.Distance(_plateCenter, Minus.transform.position) < 0.01f;
    }

    private IEnumerator WaitUntilCentered()
    {
        yield return new WaitUntil(() => _centeredSeconds >= 5);
        StopCoroutine(WaitUntilCentered());
        _centeredSeconds = 0;
        _photonView.RPC("Win", RpcTarget.All);
        SaveScores();
    }

    private void SaveScores()
    {
        DateTime now = DateTime.Now;
        string gameSessionId = Guid.NewGuid().ToString();

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            Firebase.DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                Firebase.FirebaseApp firebaseApp;
                firebaseApp = Firebase.FirebaseApp.DefaultInstance;
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                CollectionReference collectionRef = db.Collection("scores");
                foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                {
                    Dictionary<string, object> score = new Dictionary<string, object>
                {
                    { "score", GetScore() },
                    { "nickName", player.NickName },
                    { "gameId", "ElectricCharge" },
                    { "gameSessionId", gameSessionId },
                    { "dateTime", Timestamp.FromDateTime(now) },
                };
                    collectionRef.AddAsync(score);
                }
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    private string GetCongratulationMessage()
    {
        int score = GetScore();
        return score == 3
            ? "Отлично!"
            : score == 2
                ? "Хорошо!"
                : "Неплохо!";
    }

    private int GetScore()
    {
        return _integerSecondsPassed < _goodTimeLimit
            ? 3
            : _integerSecondsPassed < _normalTimeLimit
                ? 2
                : 1;
    }

    private string FormatSecondsPassed(int secondsPassed)
    {
        return string.Format("{0:D2}:{1:D2}", secondsPassed / 60, secondsPassed % 60);
    }

    private void UpdateSecondsPassed()
    {
        _secondsPassed += Time.deltaTime;
        int newIntegerSecondsPassed = (int)Math.Floor(_secondsPassed);
        if (newIntegerSecondsPassed > _integerSecondsPassed)
        {
            _integerSecondsPassed = newIntegerSecondsPassed;
            _photonView.RPC("SetSecondsPassed", RpcTarget.All, (object)newIntegerSecondsPassed);
        }
    }
}
