# Achievement_System
你好，当你看到这句话的时候，那想必你与它缘分不浅。之前我一直在做一些事，虽然可能是微不足道的，但我想记录下来，时不时可以供自己欣赏与思考，同时也可分享于他人，于是便诞生了它(AS)。

## 一、介绍

    用于管理个人人生成就的管理器，找寻自身存在的价值

## 二、使用

（一）项目框架

  项目采用C# + WPF + 人地人脸识别 + 本地 MySQL 数据库

（二）操作步骤

1.项目克隆到本地后，使用VS打开等待初始化完成

2.自行搭建数据库（可云端，可本地），准备数据库相关信息

3.首次运行时，使用特殊账户：KunKun登录配置数据库信息

4.重新运行使用即可

## 三、其他

（一）失败报错情况

1.无法找到 shape_predictor_68_face_landmarks.dat 文件

    自行网络下载放置到 models 文件后重试（文件有点大，不传仓库）

（二）数据库字段参考

    users  //用户表——用于存储用户的基本信息，包括账户、密码和人脸数据等。

|       字段        |      数据类型      |              描述              |
| :-------------: | :------------: | :--------------------------: |
|     user_id     |     int(4)     |     主键，外键，用户的唯一标识，自增 ID      |
|     account     |  varchar(24)   |             账户名              |
|    password     | VARBINARY(256) |            加密后的密码            |
|  password_salt  | VARBINARY(128) |           盐值，随机生成            |
|    face_data    |    LONGBLOB    | 人脸图像，以二进制形式存储，可为空，可存储范围0~4GB |
| face_embeddings |      BLOB      |         人脸特征向量数据，可为空         |

	achievement_categories  //成就分类表——用于存储成就的分类

|       字段       |    数据类型     |           描述            |
| :------------: | :---------: | :---------------------: |
|  category_id   |   int(4)    |    主键，分类的唯一标识，自增 ID     |
|  big_category  | varchar(16) |   字符串类型，例如 “动漫”“体育” 等   |
| small_category | varchar(16) | 字符串类型，细分类别，如 “国漫”“日漫” 等 |

	achievements  //成就表——存储成就数据

|        字段         |                数据类型                |                     描述                      |
| :---------------: | :--------------------------------: | :-----------------------------------------: |
|  achievement_id   |              int(10)               |              主键，成就的唯一标识，自增 ID               |
|      user_id      |               int(4)               |        关联到users表的用户 ID，表明该成就是属于哪个用户的        |
|    category_id    |               int(4)               | 关联到achievement_categories表的分类 ID，确定该成就所属的分类 |
| achievement_name  |            varchar(16)             |        具体的成就名称，例如 “完成《斗罗大陆》全系列观看” 等         |
|    description    |                text                |                  成就的详细描述内容                  |
| achievement_date  |                date                |                  完成该成就的日期                   |
| completion_degree | ENUM('+', '++', '+++', '++++', '') |     成就的完成程度，可为空，每一个 + 代表完成25%，null代表0%      |
|       score       |           decimal(2, 1)            |                  成就获得的评分情况                  |

（三）实际效果图

1.登录

<img width="938" height="650" alt="image" src="https://github.com/user-attachments/assets/51b380c8-f5f6-4ac1-badf-de7f37bf9fc8" />

2.首页

<img width="1485" height="1035" alt="image" src="https://github.com/user-attachments/assets/54fc5535-6679-42fe-a845-4efcd08a9a1c" />

3.庆典

<img width="1485" height="1035" alt="image" src="https://github.com/user-attachments/assets/2ec0d36c-1121-41e2-9864-252a3ab0210a" />

4.统计站

<img width="1485" height="1035" alt="image" src="https://github.com/user-attachments/assets/43c386e1-f7f0-4945-876c-bca092eb5309" />
