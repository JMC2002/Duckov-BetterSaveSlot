# Better Save Slots – Copy & Paste + 108 Save Slots

# Introduction
I actually planned to make this mod a long time ago, but since the developers added more save slots midway, I put it aside for a while.

# Features

## Quick Save Copying

The main feature is adding **Copy** and **Paste** buttons to every save slot.  
![copy](./Pic/复制.jpg)

After clicking **Copy**, you can click **Paste** on any slot to copy the save *together with its backups*.  
![paste](./Pic/粘贴.jpg)

When pasting, the game will ask whether you want to overwrite the target slot.  
![paste warning](./Pic/粘贴警告.jpg)

Pasting includes all backups, making archive management more convenient.

---

## Additional Save Slots

Go to the `JmcModLibConfig` folder under your save directory, open `BetterSaveSlot.json`, and modify the value of **“ExtraSaveSlotCount”**.  
![edit json](./Pic/修改json.png)

Or, after installing **ModSetting**, you can simply modify it in-game.  
You can add up to **102 extra slots**. Combined with the game’s 6 default slots, you can have up to **108 save slots**:  
![ModSetting](./Pic/设置界面.jpg)
![108 slots](./Pic/开108槽.jpg)

I think 108 save slots should be more than enough for most players!

# Installation
This mod requires the following dependencies:

- **[JmcModLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3613297900)**  
- **[Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=3589088839)**  

Make sure both of them load **before** this mod.

If you want to modify the extra save slot count in-game, please also install:  
- **[ModSetting](https://steamcommunity.com/sharedfiles/filedetails/?id=3595729494)**

# Notes
Deleting a save slot using this mod **does not delete the actual save files**, so you don’t need to worry about losing data if the mod stops working.

# Related Links
[GitHub Repository](https://github.com/JMC2002/Duckov-BetterSaveSlot)

[Steam Workshop Page](https://steamcommunity.com/sharedfiles/filedetails/?id=3614076662)

[Demo Video](https://www.bilibili.com/video/BV1V4URBPER7/?share_source=copy_web&vd_source=44d0c79301287bc97d360d78d8e0ec0f)

# Other
- This mod includes `.csv` localization files for all languages supported by the game.  
  If you are not satisfied with a translation, you can modify the corresponding `.csv` file in the `Lang` folder.
- For feedback and discussion, you can join the QQ group:  
  [Click to Join (617674584)](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=Kii1sz9bmNmgEgnmsasbKn6h3etgSoQR&authKey=Hni0nbFlbd%2BfDZ1GoklCdtHw4r79SuEsHvz9Pi4qs020w1f8D2DHD8EEnNN1OXo6&noverify=0&group_code=617674584)
- If you have any questions or suggestions, feel free to post in the discussion section.  
  If this mod helps you, a **Star** on GitHub and a **thumbs-up** would mean a lot to me~
