import User from '../models/User.js';
import bcrypt from 'bcryptjs';
import jwt from 'jsonwebtoken';
import nodemailer from 'nodemailer';
import crypto from 'crypto';
import mongoose from 'mongoose';

// Reusable email normalization
const normalizeEmail = (email) => email.trim().toLowerCase();

// Escape special characters in search input
const escapeRegex = (value) => {
    return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
};

// Fields that should never be exposed in API responses
const publicUserFields =
    '-password -resetOTP -resetOTPExpiry -resetOTPAttempts -resetToken -resetTokenExpiry';


// REGISTER USER
export const registerUser = async (req, res) => {
    try {
        const { name, email, password } = req.body;

        if (
            typeof name !== 'string' ||
            typeof email !== 'string' ||
            typeof password !== 'string' ||
            !name.trim() ||
            !email.trim() ||
            !password
        ) {
            return res.status(400).json({
                success: false,
                message: 'All fields are required'
            });
        }

        const normalizedEmail = normalizeEmail(email);

        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

        if (!emailRegex.test(normalizedEmail)) {
            return res.status(400).json({
                success: false,
                message: 'Invalid email format'
            });
        }

        if (password.length < 8) {
            return res.status(400).json({
                success: false,
                message: 'Password must be at least 8 characters long'
            });
        }

        const existingUser = await User.findOne({
            email: normalizedEmail
        });

        if (existingUser) {
            return res.status(409).json({
                success: false,
                message: 'User already exists'
            });
        }

        const hashedPassword = await bcrypt.hash(password, 10);

        // Role is not accepted from the client.
        // New users receive the schema default role: user.
        const user = await User.create({
            name: name.trim(),
            email: normalizedEmail,
            password: hashedPassword
        });

        return res.status(201).json({
            success: true,
            message: 'User registered successfully',
            user: {
                id: user._id,
                name: user.name,
                email: user.email,
                role: user.role
            }
        });

    } catch (error) {
        console.error('Register Error:', error);

        if (error.code === 11000) {
            return res.status(409).json({
                success: false,
                message: 'Email already exists'
            });
        }

        return res.status(500).json({
            success: false,
            message: 'Registration failed'
        });
    }
};


// LOGIN USER
export const loginUser = async (req, res) => {
    try {
        const { email, password } = req.body;

        if (
            typeof email !== 'string' ||
            typeof password !== 'string' ||
            !email.trim() ||
            !password
        ) {
            return res.status(400).json({
                success: false,
                message: 'Email and password are required'
            });
        }

        const normalizedEmail = normalizeEmail(email);

        const user = await User.findOne({
            email: normalizedEmail
        });

        if (!user) {
            return res.status(401).json({
                success: false,
                message: 'Invalid email or password'
            });
        }

        const isMatch = await bcrypt.compare(password, user.password);

        if (!isMatch) {
            return res.status(401).json({
                success: false,
                message: 'Invalid email or password'
            });
        }

        if (!process.env.JWT_SECRET) {
            throw new Error('JWT_SECRET is not configured');
        }

        const token = jwt.sign(
            {
                id: user._id.toString(),
                role: user.role
            },
            process.env.JWT_SECRET,
            { expiresIn: '1d' }
        );

        return res.status(200).json({
            success: true,
            message: 'Login successful',
            token,
            user: {
                id: user._id,
                name: user.name,
                email: user.email,
                role: user.role
            }
        });

    } catch (error) {
        console.error('Login Error:', error);

        return res.status(500).json({
            success: false,
            message: 'Login failed'
        });
    }
};


// GET LOGGED-IN USER PROFILE
export const getProfile = async (req, res) => {
    try {
        const user = await User.findById(req.user.id)
            .select(publicUserFields);

        if (!user) {
            return res.status(404).json({
                success: false,
                message: 'User not found'
            });
        }

        return res.status(200).json({
            success: true,
            message: 'Profile fetched successfully',
            user
        });

    } catch (error) {
        console.error('Get Profile Error:', error);

        return res.status(500).json({
            success: false,
            message: 'Failed to fetch profile'
        });
    }
};


// FORGOT PASSWORD - SEND OTP
export const forgotPassword = async (req, res) => {
    try {
        const { email } = req.body;

        if (typeof email !== 'string' || !email.trim()) {
            return res.status(400).json({
                success: false,
                message: 'Email is required'
            });
        }

        const normalizedEmail = normalizeEmail(email);

        const user = await User.findOne({
            email: normalizedEmail
        });

        // Do not reveal whether an account exists.
        if (!user) {
            return res.status(200).json({
                success: true,
                message: 'If the account exists, an OTP will be sent.'
            });
        }

        const otp = crypto.randomInt(100000, 1000000).toString();

        const hashedOTP = crypto
            .createHash('sha256')
            .update(otp)
            .digest('hex');

        user.resetOTP = hashedOTP;
        user.resetOTPExpiry = new Date(Date.now() + 5 * 60 * 1000);
        user.resetOTPAttempts = 0;

        // Invalidate any previously generated reset token.
        user.resetToken = undefined;
        user.resetTokenExpiry = undefined;

        await user.save();

        const transporter = nodemailer.createTransport({
            service: 'gmail',
            auth: {
                user: process.env.EMAIL_USER,
                pass: process.env.EMAIL_PASS
            }
        });

        try {
            await transporter.sendMail({
                from: process.env.EMAIL_USER,
                to: user.email,
                subject: 'Ledger-Guard Password Reset OTP',
                text: `Your password reset OTP is ${otp}. It expires in 5 minutes.`
            });

        } catch (mailError) {
            user.resetOTP = undefined;
            user.resetOTPExpiry = undefined;
            user.resetOTPAttempts = 0;
            await user.save();

            throw mailError;
        }

        return res.status(200).json({
            success: true,
            message: 'OTP sent successfully to your email'
        });

    } catch (error) {
        console.error('Forgot Password Error:', error);

        return res.status(500).json({
            success: false,
            message: 'Failed to send OTP'
        });
    }
};


// VERIFY OTP
export const verifyOTP = async (req, res) => {
    try {
        const { email, otp } = req.body;

        if (
            typeof email !== 'string' ||
            typeof otp !== 'string' ||
            !email.trim() ||
            !otp.trim()
        ) {
            return res.status(400).json({
                success: false,
                message: 'Email and OTP are required'
            });
        }

        const normalizedEmail = normalizeEmail(email);

        const user = await User.findOne({
            email: normalizedEmail
        });

        if (!user || !user.resetOTP || !user.resetOTPExpiry) {
            return res.status(400).json({
                success: false,
                message: 'Invalid or expired OTP'
            });
        }

        if (user.resetOTPExpiry.getTime() < Date.now()) {
            user.resetOTP = undefined;
            user.resetOTPExpiry = undefined;
            user.resetOTPAttempts = 0;

            await user.save();

            return res.status(400).json({
                success: false,
                message: 'OTP has expired. Please request a new OTP.'
            });
        }

        const hashedOTP = crypto
            .createHash('sha256')
            .update(otp.trim())
            .digest('hex');

        if (hashedOTP !== user.resetOTP) {
            user.resetOTPAttempts = (user.resetOTPAttempts || 0) + 1;

            if (user.resetOTPAttempts >= 5) {
                user.resetOTP = undefined;
                user.resetOTPExpiry = undefined;
                user.resetOTPAttempts = 0;

                await user.save();

                return res.status(400).json({
                    success: false,
                    message: 'Too many invalid OTP attempts. Please request a new OTP.'
                });
            }

            await user.save();

            return res.status(400).json({
                success: false,
                message: `Invalid OTP. ${5 - user.resetOTPAttempts} attempts remaining.`
            });
        }

        user.resetOTPAttempts = 0;

        const resetToken = crypto.randomBytes(32).toString('hex');

        user.resetToken = crypto
            .createHash('sha256')
            .update(resetToken)
            .digest('hex');

        user.resetTokenExpiry = new Date(Date.now() + 10 * 60 * 1000);

        user.resetOTP = undefined;
        user.resetOTPExpiry = undefined;

        await user.save();

        return res.status(200).json({
            success: true,
            message: 'OTP verified successfully',
            resetToken
        });

    } catch (error) {
        console.error('OTP Verification Error:', error);

        return res.status(500).json({
            success: false,
            message: 'OTP verification failed'
        });
    }
};


// RESET PASSWORD
export const resetPassword = async (req, res) => {
    try {
        const { resetToken, newPassword } = req.body;

        if (
            typeof resetToken !== 'string' ||
            typeof newPassword !== 'string' ||
            !resetToken ||
            !newPassword
        ) {
            return res.status(400).json({
                success: false,
                message: 'Reset token and new password are required'
            });
        }

        if (newPassword.length < 8) {
            return res.status(400).json({
                success: false,
                message: 'Password must be at least 8 characters long'
            });
        }

        const hashedToken = crypto
            .createHash('sha256')
            .update(resetToken)
            .digest('hex');

        const user = await User.findOne({
            resetToken: hashedToken,
            resetTokenExpiry: { $gt: new Date() }
        });

        if (!user) {
            return res.status(400).json({
                success: false,
                message: 'Invalid or expired reset token'
            });
        }

        user.password = await bcrypt.hash(newPassword, 10);

        user.resetToken = undefined;
        user.resetTokenExpiry = undefined;

        await user.save();

        return res.status(200).json({
            success: true,
            message: 'Password reset successfully'
        });

    } catch (error) {
        console.error('Password Reset Error:', error);

        return res.status(500).json({
            success: false,
            message: 'Password reset failed'
        });
    }
};


// GET ALL USERS - PAGINATION, SEARCH, FILTER, SORT
export const getAllUsers = async (req, res) => {
    try {
        const requestedPage = parseInt(req.query.page, 10) || 1;
        const requestedLimit = parseInt(req.query.limit, 10) || 10;

        const page = Math.max(1, requestedPage);
        const limit = Math.min(100, Math.max(1, requestedLimit));
        const skip = (page - 1) * limit;

        const sortOrder = req.query.sort === 'oldest' ? 1 : -1;

        const search =
            typeof req.query.search === 'string'
                ? req.query.search.trim()
                : '';

        const filter = {};

        if (search) {
            const safeSearch = escapeRegex(search);

            filter.$or = [
                { name: { $regex: safeSearch, $options: 'i' } },
                { email: { $regex: safeSearch, $options: 'i' } }
            ];
        }

        const role = req.query.role;

        if (role && ['admin', 'user'].includes(role)) {
            filter.role = role;
        }

        const users = await User.find(filter)
            .select(publicUserFields)
            .skip(skip)
            .limit(limit)
            .sort({ createdAt: sortOrder });

        const totalUsers = await User.countDocuments(filter);

        return res.status(200).json({
            success: true,
            message: 'Users fetched successfully',
            pagination: {
                totalUsers,
                currentPage: page,
                totalPages: Math.ceil(totalUsers / limit),
                limit
            },
            users
        });

    } catch (error) {
        console.error('Get All Users Error:', error);

        return res.status(500).json({
            success: false,
            message: 'Failed to fetch users'
        });
    }
};


// GET SINGLE USER BY ID
export const getUserById = async (req, res) => {
    try {
        const { id } = req.params;

        if (!mongoose.Types.ObjectId.isValid(id)) {
            return res.status(400).json({
                success: false,
                message: 'Invalid user ID'
            });
        }

        const user = await User.findById(id)
            .select(publicUserFields);

        if (!user) {
            return res.status(404).json({
                success: false,
                message: 'User not found'
            });
        }

        return res.status(200).json({
            success: true,
            message: 'User fetched successfully',
            user
        });

    } catch (error) {
        console.error('Get User Error:', error);

        return res.status(500).json({
            success: false,
            message: 'Failed to fetch user'
        });
    }
};


// UPDATE USER
export const updateUser = async (req, res) => {
    try {
        const { id } = req.params;
        const { name, email, role } = req.body;

        // Validate MongoDB ID
        if (!mongoose.Types.ObjectId.isValid(id)) {
            return res.status(400).json({
                success: false,
                message: 'Invalid user ID'
            });
        }

        // Prevent empty update requests
        if (
            name === undefined &&
            email === undefined &&
            role === undefined
        ) {
            return res.status(400).json({
                success: false,
                message: 'At least one field is required to update'
            });
        }

        // Validate name
        if (name !== undefined) {
            if (typeof name !== 'string' || !name.trim()) {
                return res.status(400).json({
                    success: false,
                    message: 'Name cannot be empty'
                });
            }
        }

        // Normalize email
        let normalizedEmail;

        if (email !== undefined) {
            if (typeof email !== 'string' || !email.trim()) {
                return res.status(400).json({
                    success: false,
                    message: 'Email cannot be empty'
                });
            }

            normalizedEmail = normalizeEmail(email);

            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

            if (!emailRegex.test(normalizedEmail)) {
                return res.status(400).json({
                    success: false,
                    message: 'Invalid email format'
                });
            }
        }

        // Validate role
        if (role !== undefined && !['user', 'admin'].includes(role)) {
            return res.status(400).json({
                success: false,
                message: 'Invalid role'
            });
        }

        // Find target user FIRST
        const user = await User.findById(id);

        if (!user) {
            return res.status(404).json({
                success: false,
                message: 'User not found'
            });
        }

        // Prevent admin from changing their own role
        if (
            user._id.toString() === req.user.id.toString() &&
            role !== undefined &&
            role !== user.role
        ) {
            return res.status(403).json({
                success: false,
                message: 'You cannot change your own role'
            });
        }

        // Prevent removing the last admin
        if (user.role === 'admin' && role === 'user') {
            const adminCount = await User.countDocuments({
                role: 'admin'
            });

            if (adminCount <= 1) {
                return res.status(403).json({
                    success: false,
                    message: 'Cannot remove the last admin'
                });
            }
        }

        // Check duplicate email against OTHER users only
        if (
            normalizedEmail !== undefined &&
            normalizedEmail !== user.email
        ) {
            const existingUser = await User.findOne({
                email: normalizedEmail,
                _id: { $ne: user._id }
            });

            if (existingUser) {
                return res.status(409).json({
                    success: false,
                    message: 'Email already exists'
                });
            }
        }

        // Update allowed fields only
        if (name !== undefined) {
            user.name = name.trim();
        }

        if (normalizedEmail !== undefined) {
            user.email = normalizedEmail;
        }

        if (role !== undefined) {
            user.role = role;
        }

        await user.save();

        return res.status(200).json({
            success: true,
            message: 'User updated successfully',
            user: {
                id: user._id,
                name: user.name,
                email: user.email,
                role: user.role
            }
        });

    } catch (error) {
        console.error('Update User Error:', error);

        if (error.code === 11000) {
            return res.status(409).json({
                success: false,
                message: 'Email already exists'
            });
        }

        return res.status(500).json({
            success: false,
            message: 'Failed to update user'
        });
    }
};


// DELETE USER
export const deleteUser = async (req, res) => {
    try {
        const { id } = req.params;

        if (!mongoose.Types.ObjectId.isValid(id)) {
            return res.status(400).json({
                success: false,
                message: 'Invalid user ID'
            });
        }

        const user = await User.findById(id);

        if (!user) {
            return res.status(404).json({
                success: false,
                message: 'User not found'
            });
        }

        // Prevent deleting own account
        if (user._id.toString() === req.user.id.toString()) {
            return res.status(403).json({
                success: false,
                message: 'You cannot delete your own account'
            });
        }

        // Prevent deleting the last admin
        if (user.role === 'admin') {
            const adminCount = await User.countDocuments({
                role: 'admin'
            });

            if (adminCount <= 1) {
                return res.status(403).json({
                    success: false,
                    message: 'Cannot delete the last admin account'
                });
            }
        }

        await User.findByIdAndDelete(id);

        return res.status(200).json({
            success: true,
            message: 'User deleted successfully'
        });

    } catch (error) {
        console.error('Delete User Error:', error);

        return res.status(500).json({
            success: false,
            message: 'Failed to delete user'
        });
    }
};