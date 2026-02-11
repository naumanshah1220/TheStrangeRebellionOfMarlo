# Portraits Folder

This folder contains portrait images for citizens in the database.

## File Naming Convention

Portrait files should be named using the CitizenID from the CSV database.

For example:
- `CIT001.png` - Portrait for John Smith (CitizenID: CIT001)
- `CIT002.jpg` - Portrait for Maria Garcia (CitizenID: CIT002)
- `CIT003.png` - Portrait for Robert Johnson (CitizenID: CIT003)

## Supported Formats

- PNG (.png)
- JPEG (.jpg, .jpeg)
- Any format supported by Unity's Sprite system

## Important Notes

1. The file name must exactly match the CitizenID from the CSV file
2. File extensions are case-sensitive
3. If no portrait is found for a citizen, the system will log a warning but continue functioning
4. Images will be automatically loaded as Sprites when the database initializes

## Example Structure

```
Resources/
  Portraits/
    CIT001.png
    CIT002.jpg
    CIT003.png
    ...
``` 