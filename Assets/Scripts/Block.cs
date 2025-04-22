using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum BlockType
{
    Red,
    Green,
    Blue,
    Yellow,
    Orange,
    Pink,
    Brown,
    Indigo,
    Black
}
public class Block : MonoBehaviour
{

    public BlockType type;
    private BoardManager board;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        board = FindObjectOfType<BoardManager>();
        originalColor = spriteRenderer.color;
    }

    void OnMouseDown()
    {
        board.SelectBlock(this);
    }
    public void SetSelected(bool isSelected)
    {
        spriteRenderer.color = isSelected ? Color.yellow : originalColor;
    }

}
