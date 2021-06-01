using UnityEngine;
using System.Linq;
using Photon.Pun;
using UnityEngine.InputSystem;

public class PlayerCustomProperty
{
    public static string TargetFound = "TargetFound";
}

public class Player : MonoBehaviourPunCallbacks
{
    public Material[] Materials;
    private PhotonView _photonView;
    private Vector3 _plateCenter;
    private Vector3 _rawInputMovement = Vector3.zero;

    private readonly float _playerSpeed = 0.1f;

    public void Awake()
    {
        _photonView = GetComponent<PhotonView>();
    }

    public void Update()
    {
        if (_photonView.IsMine && !GetComponent<Rigidbody>().isKinematic)
        {
            ProcessMovement();
        }
    }

    public void OnMovement(InputAction.CallbackContext value)
    {
        Vector2 inputMovement = value.ReadValue<Vector2>();
        _rawInputMovement = new Vector3(inputMovement.x, 0, inputMovement.y);
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

        if (changedProps.ContainsKey(PlayerCustomProperty.TargetFound))
        {
            bool playersFoundTarget = PhotonNetwork.PlayerList.All(GetPlayerTargetFound);
            SetActive(playersFoundTarget);
        }
    }

    [PunRPC]
    public void SetPlayerMaterialIndex(int materialIndex)
    {
        GetComponentInChildren<MeshRenderer>().material = Materials[materialIndex];
    }

    [PunRPC]
    public void SetPlateCenter(Vector3 value)
    {
        _plateCenter = value;
    }

    private void SetActive(bool value)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = value;
        }
        GetComponent<Rigidbody>().isKinematic = !value;
        transform.position = new Vector3(transform.position.x, _plateCenter.y + 0.01f, transform.position.z);
    }

    private bool GetPlayerTargetFound(Photon.Realtime.Player player)
    {
        return (bool)player.CustomProperties[PlayerCustomProperty.TargetFound];
    }

    private void ProcessMovement()
    {
        Vector3 velocity = _rawInputMovement;
        velocity = Camera.main.transform.TransformDirection(velocity);
        velocity.y = 0;
        if (velocity != Vector3.zero)
        {
            Quaternion directionRotation = Quaternion.LookRotation(velocity);
            transform.rotation = new Quaternion(transform.rotation.x, directionRotation.y, transform.rotation.z, directionRotation.w);
        }
        transform.Translate(velocity * _playerSpeed * Time.deltaTime, Space.World);
    }
}
