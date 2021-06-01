using UnityEngine;

public static class DirectionMultiplier
{
    public const int Pull = 1;
    public const int Push = -1;
}

public class Minus : MonoBehaviour
{
    public GameObject Direction;

    private GameObject _player1;
    private GameObject _player2;
    private readonly int _direction = DirectionMultiplier.Push;
    private const float _speed = 0.002f;

    public void Update()
    {
        if (_player1 == null || _player2 == null)
        {
            TryAssignPlayers();
        }
        else
        {
            if (!_player1.GetComponent<Renderer>().enabled || !_player2.GetComponent<Renderer>().enabled)
            {
                return;
            }
            Vector3 nextPositionFromPlayer1 = CalculateNextPositionRelativeToPlayer(_player1);
            Vector3 nextPositionFromPlayer2 = CalculateNextPositionRelativeToPlayer(_player2);
            Vector3 nextPosition = Vector3.Lerp(nextPositionFromPlayer1, nextPositionFromPlayer2, 0.5f);
            UpdateArrow(nextPosition);
            Vector3 direction = (nextPosition - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, float.MaxValue - 1);
            transform.position = nextPosition;
        }
    }

    private void TryAssignPlayers()
    {
        GameObject[] playersByTag = GameObject.FindGameObjectsWithTag("Player");
        if (playersByTag.Length == 2)
        {
            _player1 = playersByTag[0];
            _player2 = playersByTag[1];
        }
    }

    Vector3 CalculateNextPositionRelativeToPlayer(GameObject player)
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float playerAffectSpeed = _speed / distanceToPlayer;
        float step = playerAffectSpeed * _direction * Time.deltaTime;
        Vector3 pointToMoveFor = Vector3.Lerp(player.transform.position, player.transform.position, 0.5f);

        return Vector3.MoveTowards(transform.position, pointToMoveFor, step);
    }

    private void UpdateArrow(Vector3 nextPosition)
    {
        float dPosition = transform.position.magnitude / nextPosition.magnitude;
        Direction.transform.localScale = new Vector3(dPosition, Direction.transform.localScale.y, Direction.transform.localScale.z);
        // Arrow.transform.localPosition = new Vector3(0, 0, Arrow.GetComponent<MeshRenderer>().bounds.extents.z);
    }
}
