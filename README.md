working with Postman

first command for the client and server handshake
```
{
    "protocol":"json",
    "version":1
}
```

```CreateUser``` - method for creating new user
```
{
    "type": 1,
    "target": "CreateUser",
    "arguments": [{
        "Username" : "NewUsername",
        "Password" : "NewUserPasswood",
    }]
}
```

```JoinChat``` - method for adding user to the chat. One User can only be in one chat.
```
{
    "type": 1,
    "target": "JoinChat",
    "arguments": [{
        "Username" : "ExistingUsername",
        "Password" : "ExistingUserPasswood",
        "Chat" : "ExistingChatname"
    }]
}
```

```Send``` - method for sending message to the chat, the user has joined.
```
{
    "type": 1,
    "target": "Send",
    "arguments": [{
        "Username" : "ExistingUsername",
        "Message" : "Message"
    }]
}
```

```LeaveChat``` - method to exit the chat, to which the user was joined.
```
{
    "type": 1,
    "target": "LeaveChat",
    "arguments": [{
        "Username" : "ExistingUsername",
    }]
}
```

'''CreateChat''' -  method to creating new chat and assigning an administrator to the chat creator.
```
{
    "type": 1,
    "target": "CreateChat",
    "arguments": [{
        "Username" : "ExistingUsername",
        "Password" : "ExistingUserPasswood",
        "Chat" : "NewChatname"
    }]
}
```

'''DeleteChat''' -  method to delete chat (chat can delete only admin user)
```
{
    "type": 1,
    "target": "DeleteChat",
    "arguments": [{
        "Username" : "AdminUsername",
        "Password" : "AdminUserPasswood",
        "Chat" : "ExistingChatname"
    }]
}
```

