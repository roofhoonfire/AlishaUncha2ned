using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SkillTileDatabase
{
    public static List<List<Vector3Int>> skillShapes = new()
    {
        new List<Vector3Int> { new(1, -1, 0), new(2, -2, 0) ,new(1,-2,1)}, //만나자마자, 세로베기
        
        new List<Vector3Int> {new Vector3Int(1, -1, 0),
            new Vector3Int(2, -2, 0),
            new Vector3Int(1, -2, 1),
            new Vector3Int(2, -1, -1)}
        ,


        new List<Vector3Int> {
            new Vector3Int(3,-2,-1), //리벤지
            new Vector3Int(2, -2, 0),
            new Vector3Int(2, -3, 1),
            new Vector3Int(2, -1, -1),
            new Vector3Int(1, -2, 1)
        },

        new List<Vector3Int> {
             new(1, -1, 0), new(2, -2, 0)
        },

        new List<Vector3Int> { //조악한 투척
            new (0,0,0),
        },

        new List<Vector3Int> //공중강습
        {
              new(0, 0, 0),
              new(1, 0, -1),
              new(-1, 0, 1),
              new(0, 1, -1),
              new(0, -1, 1),
              new(1, -1, 0),
              new(-1, 1, 0),

        },
          new List<Vector3Int> //파이어볼
        {
              new(1, -1, 0),
              new(2, -2, 0),
              new(3, -3, 0),
              new(3, -4, 1),
              new(2, -3, 1),

        },


           new List<Vector3Int> //프로즌
        {
              new(1, -1, 0),
              new(2, -2, 0),
              new(1, -2, 1),
              new(1, -3, 2),
              new(2 ,-1, -1),
                new(3, -1, -2),
        }
           ,
           new List<Vector3Int> //썬브
        {
              new(1, -1, 0),
              new(2, -2, 0),
              new(3, -3, 0),
              new(3, -4, 1),
              new(2, -3, 1),
             new(4, -4, 0),
             new(5, -5, 0),


        },

                 new List<Vector3Int> //트랩언더아이스
        {
              new(1, -1, 0),
              new(2, -2, 0),
              new(1, -2, 1),
              new(1, -3, 2),
              new(2 ,-1, -1),
                new(3, -1, -2),
                new(3,-3,0),
        }
    };
}