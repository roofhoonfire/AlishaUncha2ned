using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


public static class HexSkill
{//각도 음수 되면 피격 범위 180도 뒤집어야함  ㅠ,ㅠ,ㅠ
    // 60° 시계 방향 회전 (큐브 좌표)
    // (x, y, z) → (–z, –x, –y)
    public static Vector3Int RotateCW60(Vector3Int c)
    {
        return new Vector3Int(-c.z, -c.x, -c.y);
    }

    // 기본 우(→) 방향부터 시계방향으로 정의한 6개 이웃 벡터
    // index 0: 우, 1: 우하, 2: 좌하, 3: 좌, 4: 좌상, 5: 우상
    public static readonly Vector3Int[] Directions = new Vector3Int[]
    {
        new Vector3Int( 1, -1,  0),   // 0: 우
        new Vector3Int(-1,  0,  1),   // 1: 우하  (교체됨)
        new Vector3Int( 0,  1, -1),   // 2: 좌하
        new Vector3Int(-1,  1,  0),   // 3: 좌
        new Vector3Int( 1,  0, -1),   // 4: 좌상  (교체됨)
        new Vector3Int( 0, -1,  1),   // 5: 우상
    };

    // 기본 모양을 rotateCount 만큼 시계 방향으로 회전
    public static List<Vector3Int> RotateShape(List<Vector3Int> defaultShape, int rotateCount)
    {
        var result = new List<Vector3Int>(defaultShape.Count);
        foreach (var c in defaultShape)
        {
            var r = c;
            for (int i = 0; i < rotateCount; i++)
                r = RotateCW60(r);
            result.Add(r);
        }
        return result;
    }

    // 스킬 타격 타일 계산
    // defaultShape: 우 방향 기준 상대좌표 리스트
    // dir: 실제 발동할 방향 벡터 (위 Directions 중 하나)
    // origin: 캐릭터 현재 위치(큐브 좌표)
    public static List<Vector3Int> GetSkillTargets(
        List<Vector3Int> defaultShape,
        Vector3Int dir,
        Vector3Int origin)
    {
        int defaultIndex = 0; // “우”가 기준
        int targetIndex = Array.IndexOf(Directions, dir);
        if (targetIndex < 0)
            throw new ArgumentException($"지원되지 않는 방향입니다: {dir}");

        // 시계 방향 회전 보정 (Directions 순서와 RotateCW60 매칭)
        int rotateCount = (defaultIndex - targetIndex + 6) % 6;

        var rotated = RotateShape(defaultShape, rotateCount);
        return rotated.Select(v => v + origin).ToList();
    }
    public static List<Vector3Int> GetSkillAreaByClickedTile(List<Vector3Int> defaultShape, int clickedTileIndex)
    {
        Vector3Int clickedCoord = GridManagement.Instance.GetCoordFromIndex(clickedTileIndex);
        return defaultShape.Select(offset => clickedCoord + offset).ToList();
    }
    public static List<Vector3Int> GetTilesBetweenPlayerAndTarget(Vector3Int playerCoord, Vector3Int targetCoord)
    {
        Vector3Int? matchedDir = null;

        foreach (var dir in Directions)
        {
            Vector3Int current = playerCoord;

            while (true)
            {
                current += dir;
                if (current == targetCoord)
                {
                    matchedDir = dir;
                    break;
                }

                if (!GridManagement.Instance.coordToIndex.ContainsKey(current))
                    break;
            }

            if (matchedDir.HasValue)
                break;
        }

        if (!matchedDir.HasValue)
            return new List<Vector3Int>();

        // 방향 확인 완료 → 그 방향으로 playerCoord 다음부터 targetCoord까지 타일 모으기
        Vector3Int dirToUse = matchedDir.Value;
        Vector3Int curCoord = playerCoord + dirToUse;

        List<Vector3Int> result = new();

        while (true)
        {
            if (!GridManagement.Instance.coordToIndex.ContainsKey(curCoord))
                break;

            result.Add(curCoord);

            if (curCoord == targetCoord)
            
                break;
            
            curCoord += dirToUse;
        }
        // targetCoord를 맨 앞으로 이동
        if (result.Count > 0)
        {
            result.Remove(targetCoord);
            result.Insert(0, targetCoord);
        }
        return result;
    }

}
public class SkillAreaCalc : MonoBehaviour
{
    // Start is called before the first frame update
    

}
