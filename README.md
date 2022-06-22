# About

This tool allows you to setup world-locked items on your avatar,
meaning that placed items won't move until you put them away.
While plenty of documentation exist on how to setup items that way,
the process is still tedious, so I made a tool that :

* Setup the hierarchy.
* Create the animations.
* Setup the FX animator (creating it if it's doesn't exist).
* Setup the SDK3 menu parameters.
* Add the SDK3 menu buttons.

The tool will copy the avatar, and any modified asset, and will
perform modifications on these copies only.

Still, as always, backup the items and avatars you'll use with
this tool.
I only tested it on MY avatars.
So far, so long, no problems arised, but you might encounter bugs
I haven't.

Locking items with particles work with Quest avatars, but it still
pretty limited.
If you don't want to have a "Very Poor" rated avatar, the limits are
200 polygons for Quest users, 2500 for PC users.

Locking items with constraints has no such limitations but only work
on PC at the moment.

# Requirements

* Unity
* VRChat Avatar SDK 3.0

# Download

See https://github.com/vr-voyage/vrchat-worldlock-autosetup/releases

# Install

* Either install [**ObjectsFixer.unitypackage**](https://github.com/vr-voyage/vrchat-worldlock-autosetup/releases/download/v1.2/ObjectWorldLocker-Japanese-Default.unitypackage)
  using the editor menu **Assets > Import Package... > Custom Package...**.

**OR**

* Copy the code of this repository inside the **Assets** of your SDK3 Avatar project.

# Items in the demo

* Avatar : Vケットちゃん１号  
  https://www.v-market.work/ec/items/656/detail/
  
* Item : Low poly (midi) keyboard  
  https://sketchfab.com/3d-models/low-poly-midi-keyboard-50040134cbfe4268a9af8b074bd24b15

# Usage

## Setup an avatar with one item locked using Particles

Works with PC and Quest avatars.

https://user-images.githubusercontent.com/84687350/128454218-0d64153f-094c-4279-8635-006793f7c4da.mov

* Add your avatar to the Scene root.
* For good measures, make sure that your avatar position and rotation are set to 0,0,0.
* Add the item to the Scene root.
* Move the added item where you'd like to see it spawned, in relation to your avatar.

* Open the setup window **Voyage > World Lock Setup - Particles (PC or Quest)**.
* Click on the 'RESET' button, at the upper left of that window.
* Drag your avatar and the item to setup from the **Hierarchy**, and drop them inside that window.
* Click on the 'APPLY' button, that should have appeared at the bottom of that window.

Once done, a new setup clone of the avatar will appear.

You can then upload the clone, and use it in-game to spawn the objects using the **Expression menu**.

https://user-images.githubusercontent.com/84687350/128454266-9a728bc1-1f2e-4f90-93c9-e9a5683ed453.mov

This new clone main menu will have two new buttons in its **Expression menu** :
* Spawn - Which allows you to spawn the actual item.
* Rotate - Which allows you to define the orientation of the spawned element.

All the animations, animators, materials and meshes will be copied into a specific folder, named :
"[Avatar name]-ParticlesLock-[YearMonthDay]-[HourMinSeconds]-[Milliseconds]".
You can define where the folder is generated by dropping a folder on "Save Dir".

> You can actually drop a saved asset on "Save Dir".
> If you do so, "Save Dir" will be set to that asset folder.

Note :
- Due to how the Particle Emitter works,
  the orientation of the spawned item ignores the orientation of the
  avatar.  
  That means that spawned item will always have the same orientation.  
  This is why a "Rotate" button was added, to change the spawned
  orientation.
- At best, Quest avatars will see their rating drop to **Poor** when
  using a Particle Emitter.  
  If the spawned item has more than 200 (two hundred) polygons,
  the rating will automatically downgrade to **Very Poor**.  
  You should take this seriously when it comes to Quest Avatars,
  since Quest clients are setup to always display "Fallback" avatars
  instead of Very Poor avatars.  
  Meaning that other users won't see your setup avatar unless they
  manually choose "Show avatar".
- You can only use ONE material to define the look of the spawned item.
  Note that, weirdly enough, you CANNOT use Particles shaders with
  Particles systems on Quest avatars.  
  Which means : No transparency.

## Setup an avatar with items locked using Constraints

Works with PC avatars only.
**Constraints** components are not allowed on Quest avatars.

https://user-images.githubusercontent.com/84687350/128454290-41b096f6-5657-4ec5-a816-60d50565aba4.mov

* Add your avatar to the Scene root.
* For good measures, make sure that your avatar position and rotation
  are set to 0,0,0.
* Add the items to the Scene root.
* Set the rotation and position of the items, in relation to your
  avatar, to define how it should appear when shown.
* The **Expression menu** buttons will show the item names as set
  in the **Hierarchy**.  
  So, set them accordingly.

* Open the setup window
  **Voyage > World Lock Setup - Constraints (PC)**.
* Click on the 'RESET' button, at the upper left of that window.
* Drag your avatar and the item to setup from the **Hierarchy**,
  and drop them inside that window.
* Click on the 'APPLY' button, that should have appeared at the
  bottom of that window.

You can then upload the clone, and use it in-game to spawn the
items you just setup.

https://user-images.githubusercontent.com/84687350/128454320-78cb2eb1-1556-4a7e-888d-b442904e48ae.mov

This new clone will have a new submenu button "World Objects"
with Toggle buttons allowing you to spawn each individual object.

Note:
- You can setup up to 8 individual items (Only 8 items for now,
  due to menu creation limitations).
- The object orientation is relative to player orientation
  spawning it.  
  Since the object is then locked, the base rotation won't change
  until you spawn it again.

### Setup the items to always show at a specific world position

Follow **Setup an avatar with items locked using Constraints**,
but before clicking on 'APPLY' in the setup window, check
the box **Lock from world (0,0,0)**.  
By doing so, the avatar will be setup so that the items always
spawn at specific world coordinates.

These coordinates being the "Position" of these items, in the
Editor, before clicking on 'APPLY'.

That means that you can setup a **World Overlay** using this
mechanism.
  
# Licence
MIT

Meaning that, yes, You are free to incorporate this tool,
or parts of it, into other commercial goods, open source projects, ...

# 使い方

## Particle Systemでアイテムを固定する機能をアバターに追加する手順

PCとQuestアバター対応

* まずは、SceneでSDK3.0のアバターを用意します。
* アイテムを配置する時に、そのアイテムの位置がズレないように、
  アバターの位置と回転度をゼロにします。
* 後は、Sceneで固定したいアイテムを追加します。
* アバターを基準にして、アイテムの位置を設定します。
* メニューバーで「Voyage > World Lock Setup - Particles (PC or Quest)」を押下して、
  自動設定画面を開きます。
* 画面の左上にある「リセット」ボタンを押します。
* Hierarchyからアバターとアイテムをドラッグして、その画面の上にドロップします。
* 画面の下に現れた「適用」ボタンを押します。

すると、アバターがコピーされて、そのコピーは設定されて、Sceneに追加されます。  
そのコピーをアップロードしたら、ゲーム内で、アイテムをワールドで召喚できます。  
召喚したアイテムは、削除するまで、動きません。

そのためにExpressionsメニューで、二つのボタンが追加されます：
* Spawn - アイテムを召喚・削除するボタン。
* Rotate - アイテムの回転度を決めるボタン。

> 回転度を変わると、配置場所がリセットされます。

> アバターの方向を替えっても、召喚するアイテムの方向が変わりません。  
> パーティクルで配置されているアイテムの方向は
> 「Rotate」ボタンで決めます。


## Constraints (束縛)でアイテムを固定する機能をアバターに追加する手順

PCアバター対応
「Constraint」のコンポネントはQuest対応アバターで使えません。

* まずは、SceneでSDK3.0のアバターを用意します。
* アイテムの配置場所がズレないように、
  アバターの位置と回転度をゼロにします。
* 後は、Sceneに固定したいアイテムを追加します。
* アバターを基準にして、アイテムの回転と位置を設定します。
* 後は、メニューバーで「Voyage > World Lock Setup - Constraints (PC)」を押下して、
  自動設定ツールの画面を開きます。
* 画面の左上にある「リセット」ボタンを押します。
* Hierarchyからアバターとアイテムをドラッグして、その画面の上にドロップします。
* 画面の下に現れた「適用」ボタンを押します。

すると、アバターがコピーされて、そのコピーは設定されて、Sceneに追加されます。  
そのコピーをアップロードしたら、ゲーム内で、アイテムをワールドに配置できます。  
配置したアイテムは、収納されるまで、動きません。

そのために、Expressionsメニューで、「World Objects」と言うサブメニューが追加されます。  
そこで、それぞれのアイテムを出すために、ボタンがあります。  
アイテムのボタンを押下すると、そのアイテムを配置します。  
もう一度押下すると、そのアイテムを収納します。

# メリット・デメリット

## Particle System

* **+** Questでも使えます。
* **-** QuestのアバターのRatingがPoorになります、必ず。
* **-** Questのアバターで200個のポリゴン以上の物を召喚すると、RatingがVery poorになって、相手がShow Avatarしないと、ロボットのアバターしか見えなくなります。
* **-** パーティクル・システムのレンダラーは一個のマテリアルしか使えませんから、召喚したい物の見た目を一個のマテリアルで決めなければなりません。
* **-** Questのアバターで、意外と、Particles SystemはParticlesのシェイダーを使えません。ですので、透明化はできません。

## Constraints (束縛)

* **-** PC Only
* **+** Ratingを低下しません。
* **+** アバターの一部ですから、アバターが使えるComponentをアイテムに付けられます　(音楽、アニメーション、…）。

# 質問

## 生成されたファイルの名前を替えることができますか？
はい、ファイルの名前を替えっていいです。

## 生成された名前を変化してよろしいですか？
はい、名前を替えってよろしいです。

## 生成されたコピーのメニューを編集できますか？
メニュー・アイテムの名前を替えっても平気です。
そのパラメータの名前を変わると、そのアイテムが動作出来なくなる可能性がありますから、ご遠慮ください。

## 新しいアイテムを追加するために、アバターのコピーをもう一度ツールで使うことはできますか
できません。  
新しいアイテムを追加したいなら、手順を元からやり直してください。  
そのツールはオリジナル・アバターと既に設定されているアバターを区別できません。  
そのせいで、既に設定されているアバターを、もう一度ツールで設定すると、AnimatorのParameterの名前が混ざって、問題が発生します。
