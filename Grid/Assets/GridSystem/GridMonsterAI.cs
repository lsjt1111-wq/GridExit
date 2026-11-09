
using UnityEngine;
using System.Collections.Generic;
using GridTutorial;

[RequireComponent(typeof(GridMover), typeof(GridOccupant))]
public class GridMonsterAI : MonoBehaviour
{
    private GridMover gridMover;
    private GridOccupant gridOccupant;

    [Header("Monster Movement")]
    [SerializeField] private float moveInterval = 1.5f;

    private float moveTimer = 0f;

    // 상하좌우 이동 방향
    private Vector2Int[] directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    void Awake()
    {
        gridMover = GetComponent<GridMover>();
        gridOccupant = GetComponent<GridOccupant>();
    }

    void Update()
    {
        // Board가 없거나 현재 이동 중이면 대기
        if (gridOccupant == null ||
            !gridOccupant.Board ||
            gridMover.IsMoving)
        {
            return;
        }

        // 시간 누적
        moveTimer += Time.deltaTime;

        // 이동 시간이 지났는지 확인
        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;

            MoveRandomly();
        }
    }

    void MoveRandomly()
    {
        // 이동 가능한 방향 후보 생성
        List<Vector2Int> availableDirections =
            new List<Vector2Int>(directions);

        while (availableDirections.Count > 0)
        {
            // 랜덤 방향 선택
            int randomIndex = Random.Range(
                0, availableDirections.Count);

            Vector2Int selectedDirection =
                availableDirections[randomIndex];

            // 이미 선택한 방향 제거
            availableDirections.RemoveAt(randomIndex);

            // 현재 위치 + 이동 방향 = 목적지 좌표
            Vector2Int destination =
                gridOccupant.Cell + selectedDirection;

            // 목적지로 이동 시도
            if (gridMover.MoveTo(destination))
            {
                return;
            }
        }

        // 4방향 모두 이동할 수 없으면 제자리 대기
    }
}
