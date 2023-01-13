<div align="center">

  <a href="https://www.re-evolution.io"><img height="170x" src="https://avatars.githubusercontent.com/u/118198999?s=200&v=4" /></a>
  <h1>Unity x Solana Wallet Link</h1>
  <p>
    <strong>A PC native solution for interacting with Solana Wallets</strong>
  </p>
  <p>
    <a href="https://opensource.org/licenses/MIT"><img alt="License" src="https://img.shields.io/github/license/DBGames-Ltd/Unity-Wallet-Link?color=red" /></a>
  </p>
</div>

This repository contains a client side package which can be used to interact with browser based Solana Wallet Providers from the Unity Game Engine.
There are many packages around which support WebGL and Mobile wallet connections, this intention of this package is to create an easier access point for developing PC native games on Solana.

## ⚠ Important Notes
- This package is being adapted to become part of the [Solana.Unity SDK](https://github.com/garbles-labs/Solana.Unity-SDK) by [Garbles Labs](https://github.com/garbles-labs), and its interfaces will be adjusted accordingly.
- Using this package currently requires that you run a Web App with the following capabilities:
  - Can connect to Solana Wallets and Sign Messages
  - Can collect a user's public key, message signature, and encoded message.
  - Can send a post request to localhost at a port specified in the query string.
- We are in the process of creating a template React App respository for the functionality specified above.

## [Documentation]()
⚠ WIP ⚠

## Dependencies
  - Newtonsoft.Json
  - Chaos.NaCl.Standard